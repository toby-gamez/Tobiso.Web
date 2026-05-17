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
            // Choose the most appropriate version: prefer highest grade-level if available, else first.
            var versionContent = post.Versions?.OrderByDescending(v => v.GradeId.HasValue ? v.GradeId.Value : int.MinValue)
                .FirstOrDefault()?.Content ?? string.Empty;
            var names = await _aiService.DetectPeopleInTextAsync(versionContent);
            return Ok(names);
        }

        [HttpPost("grammar-check")]
        [AllowAnonymous]
        public async Task<IActionResult> GrammarCheck([FromBody] GrammarCheckRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Missing content");

            try
            {
                var resp = await _aiService.CheckGrammarAsync(request.Content);
                // If AI is unavailable locally (SSL or network issues), the service may return an empty response.
                // Return OK with empty issues so the admin UI can continue working without blocking post edits.
                return Ok(resp ?? new GrammarCheckResponse());
            }
            catch (Exception ex)
            {
                // Log but do not fail the request — fall back to empty result so editor remains usable offline.
                Serilog.Log.Warning(ex, "Grammar check failed; returning empty issues to keep editor usable");
                return Ok(new GrammarCheckResponse());
            }
        }

        [HttpGet("person")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPerson([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Missing name");

            var systemPrompt = _configuration["OpenAI:PersonSystemPrompt"]
                ?? "You are a factual knowledge assistant that generates person information cards. Respond ONLY with a raw JSON object — no markdown, no prose, no code fences. For fields you are not certain about use null for numeric fields and an empty string for text fields. Do not invent or speculate.";

            var userPrompt = $"Return a JSON object for the person \"{name}\" with exactly these keys: " +
                "name (string, full name), " +
                "role (string, short description, e.g. \"Czech composer and pianist\"), " +
                "birthYear (integer or null), " +
                "deathYear (integer or null), " +
                "bio (string, 2-3 factual sentences), " +
                "externalLink (string, Wikipedia URL or empty string).";

            try
            {
                var raw = await _aiService.AskRawJsonAsync(systemPrompt, userPrompt);

                if (string.IsNullOrWhiteSpace(raw))
                {
                    Serilog.Log.Warning("Person generation returned empty response for {Name}", name);
                    return StatusCode(502, new { message = "AI returned an empty response" });
                }

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(raw);
                }
                catch (JsonException ex)
                {
                    Serilog.Log.Error(ex, "Person JSON parse failed for {Name}. Raw: {Raw}", name, raw);
                    return StatusCode(502, new { message = "AI response was not valid JSON", raw });
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
                    Name         = string.IsNullOrEmpty(GetProp("name")) ? name : GetProp("name"),
                    Bio          = GetProp("bio"),
                    Role         = GetProp("role"),
                    BirthYear    = GetInt("birthYear"),
                    DeathYear    = GetInt("deathYear"),
                    ExternalLink = GetProp("externalLink"),
                    AiGenerated  = true
                };
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Person generation failed for {Name}", name);
                return StatusCode(502, new { message = ex.Message });
            }
        }
    }
}
