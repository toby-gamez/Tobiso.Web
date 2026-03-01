using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.App.Services;
using Microsoft.AspNetCore.Authorization;

namespace Tobiso.Web.Api.Controllers
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
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var limit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;

            var remainingBefore = _rateLimitService.GetRemaining(ip, limit);
            if (remainingBefore <= 0)
            {
                return StatusCode(429, new { message = "Daily limit reached" });
            }

            // consume
            var allowed = _rateLimitService.TryConsume(ip, limit);
            if (!allowed)
            {
                return StatusCode(429, new { message = "Daily limit reached" });
            }

            var resp = await _aiService.AskAsync(request, ip);
            resp.RemainingQuestions = _rateLimitService.GetRemaining(ip, limit);
            return Ok(resp);
        }
    }
}
