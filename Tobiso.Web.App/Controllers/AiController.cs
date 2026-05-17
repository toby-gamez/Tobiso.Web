using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tobiso.Web.Shared.DTOs;
using System.Text.Json;
using Tobiso.Web.App.Services;
using Tobiso.Web.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Tobiso.Web.App.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly Tobiso.Web.Shared.Interfaces.IAiService _aiService;
        private readonly IAiRateLimitService _rateLimitService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Tobiso.Web.Api.Services.IPostService _postService;

        public AiController(Tobiso.Web.Shared.Interfaces.IAiService aiService, IAiRateLimitService rateLimitService, IConfiguration configuration, IHttpClientFactory httpClientFactory, Tobiso.Web.Api.Services.IPostService postService)
        {
            _aiService = aiService;
            _rateLimitService = rateLimitService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _postService = postService;
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

        [HttpGet("detect-persons/{postId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DetectPersons(int postId)
        {
            var post = await _postService.GetById(postId);
            if (post == null) return NotFound();
            var names = await _aiService.DetectPeopleInTextAsync(post.Content ?? string.Empty);
            return Ok(names);
        }

        [HttpGet("person")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPerson([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Missing name");
            var aiReq = new Tobiso.Web.Shared.DTOs.AiChatRequest
            {
                PostId = 0,
                Question = $"Provide a short factual card for the person named '{name}'. Return JSON with keys: name, role, birthYear, deathYear, bio, externalLink, photoUrl."
            };

            try
            {
                var aiResp = await _aiService.AskAsync(aiReq, "person-gen");
                var raw = aiResp?.Answer ?? string.Empty;

                // Try to extract the first JSON object in the AI response. Be tolerant of extra text.
                string jsonToParse = string.Empty;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var start = raw.IndexOf('{');
                    var end = raw.LastIndexOf('}');
                    if (start >= 0 && end > start)
                        jsonToParse = raw.Substring(start, end - start + 1);
                    else
                        jsonToParse = raw.Trim();
                }

                JsonDocument doc;
                try
                {
                    doc = string.IsNullOrEmpty(jsonToParse) ? JsonDocument.Parse("{}") : JsonDocument.Parse(jsonToParse);
                }
                catch
                {
                    // Fallback: try to find a JSON object via regex and parse it
                    var m = System.Text.RegularExpressions.Regex.Match(raw, @"\{[\s\S]*\}");
                    if (m.Success)
                    {
                        try { doc = JsonDocument.Parse(m.Value); }
                        catch { doc = JsonDocument.Parse("{}"); }
                    }
                    else
                    {
                        doc = JsonDocument.Parse("{}");
                    }
                }

                var root = doc.RootElement;

                string GetProp(string prop) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() ?? string.Empty : string.Empty;
                int? GetInt(string prop)
                {
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null)
                    {
                        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
                        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var j)) return j;
                    }
                    return null;
                }

                var resp = new Tobiso.Web.Shared.DTOs.PersonResponse
                {
                    Name = string.IsNullOrEmpty(GetProp("name")) ? name : GetProp("name"),
                    Slug = GetProp("slug"),
                    Bio = GetProp("bio"),
                    Role = GetProp("role"),
                    BirthYear = GetInt("birthYear"),
                    DeathYear = GetInt("deathYear"),
                    ExternalLink = GetProp("externalLink"),
                    PhotoUrl = GetProp("photoUrl"),
                    AiGenerated = true
                };
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Person generation failed for {Name}", name);
                return StatusCode(502, new { message = "AI generation failed" });
            }
        }
    }
}
