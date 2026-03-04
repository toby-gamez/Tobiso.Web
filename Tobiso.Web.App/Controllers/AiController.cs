using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.App.Services;
using Microsoft.AspNetCore.Authorization;

namespace Tobiso.Web.App.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IAiRateLimitService _rateLimitService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AiController(IAiService aiService, IAiRateLimitService rateLimitService, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _aiService = aiService;
            _rateLimitService = rateLimitService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("diag")]
        [AllowAnonymous]
        public async Task<IActionResult> Diag()
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return Problem(detail: "OpenAI:ApiKey not configured", statusCode: 500);
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var res = await client.GetAsync("https://api.openai.com/v1/models");
                var body = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode)
                {
                    return Ok(new { status = "ok", httpStatus = (int)res.StatusCode });
                }
                return StatusCode(502, new { status = "bad_gateway", httpStatus = (int)res.StatusCode, body = body.Length > 1000 ? body.Substring(0, 1000) + "..." : body });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI diag failed");
                var inner = ex.InnerException?.Message;
                return StatusCode(502, new { status = "error", message = ex.Message, inner = inner });
            }
        }

        [HttpPost("ask")] 
        [AllowAnonymous]
        public async Task<IActionResult> Ask([FromBody] AiChatRequest request)
        {
            // identify caller: prefer X-Client-Id header (for trusted apps), fallback to IP
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string clientId = null;
            if (Request.Headers.TryGetValue("X-Client-Id", out var vals))
            {
                clientId = vals.FirstOrDefault();
            }

            var rateKey = string.IsNullOrEmpty(clientId) ? ip : $"client:{clientId}";

            // determine limit: per-client override in config -> OpenAI:ClientLimits:{clientId}
            int limit;
            if (!string.IsNullOrEmpty(clientId))
            {
                var confVal = _configuration[$"OpenAI:ClientLimits:{clientId}"];
                if (!string.IsNullOrEmpty(confVal) && int.TryParse(confVal, out var clientLimit))
                {
                    limit = clientLimit;
                }
                else
                {
                    limit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
                }
            }
            else
            {
                limit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            }

            var remainingBefore = _rateLimitService.GetRemaining(rateKey, limit);
            if (remainingBefore <= 0)
            {
                return StatusCode(429, new { message = "Daily limit reached" });
            }

            // consume
            var allowed = _rateLimitService.TryConsume(rateKey, limit);
            if (!allowed)
            {
                return StatusCode(429, new { message = "Daily limit reached" });
            }

            var resp = await _aiService.AskAsync(request, rateKey);
            resp.RemainingQuestions = _rateLimitService.GetRemaining(rateKey, limit);
            return Ok(resp);
        }
    }
}
