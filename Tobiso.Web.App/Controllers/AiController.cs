using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tobiso.Web.Shared.DTOs;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tobiso.Web.App.Services;
using Tobiso.Web.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

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
        private readonly IAiChatHistoryService _chatHistory;
        private readonly IUserService _userService;

        public AiController(Tobiso.Web.Shared.Interfaces.IAiService aiService, IAiRateLimitService rateLimitService, IConfiguration configuration, IHttpClientFactory httpClientFactory, Tobiso.Web.Api.Services.IPostService postService, IAiChatHistoryService chatHistory, IUserService userService)
        {
            _aiService = aiService;
            _rateLimitService = rateLimitService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _postService = postService;
            _chatHistory = chatHistory;
            _userService = userService;
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

            // Per-device rate key using X-Device-Id; fall back to IP
            string deviceId = null;
            if (Request.Headers.TryGetValue("X-Device-Id", out var dvVals))
                deviceId = dvVals.FirstOrDefault();

            var rateKey = !string.IsNullOrEmpty(deviceId) ? $"device:{deviceId}" : ip;

            // Determine base limit from X-Client-Id config override
            string clientId = null;
            if (Request.Headers.TryGetValue("X-Client-Id", out var vals))
                clientId = vals.FirstOrDefault();

            int baseLimit;
            if (!string.IsNullOrEmpty(clientId))
            {
                var confVal = _configuration[$"OpenAI:ClientLimits:{clientId}"];
                baseLimit = !string.IsNullOrEmpty(confVal) && int.TryParse(confVal, out var cl) ? cl
                    : int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            }
            else
            {
                baseLimit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            }

            var effectiveLimit = baseLimit + _rateLimitService.GetBonusTotal(rateKey);

            var remainingBefore = _rateLimitService.GetRemaining(rateKey, effectiveLimit);
            if (remainingBefore <= 0)
                return StatusCode(429, new { message = "Daily limit reached" });

            var allowed = _rateLimitService.TryConsume(rateKey, effectiveLimit);
            if (!allowed)
                return StatusCode(429, new { message = "Daily limit reached" });

            var resp = await _aiService.AskAsync(request, rateKey);
            resp.RemainingQuestions = _rateLimitService.GetRemaining(rateKey, effectiveLimit);

            // For logged-in students: deduct 1 credit and save to chat history
            if (User.FindFirst("role")?.Value == "student"
                && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var studentId))
            {
                var deducted = await _userService.DeductCreditsAsync(studentId, 1, "ai_ask");
                if (!deducted)
                    return StatusCode(402, new { message = "Nemáš dostatek kreditů." });

                var session = await _chatHistory.GetOrCreateSessionAsync(studentId, request.PostId);
                await _chatHistory.SaveMessageAsync(session.Id, "user", request.Question ?? "");
                await _chatHistory.SaveMessageAsync(session.Id, "assistant", resp.Answer ?? "", creditsUsed: 1);
            }

            return Ok(resp);
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var sessions = await _chatHistory.GetUserSessionsAsync(userId);
            return Ok(sessions.Select(s => new
            {
                s.Id,
                s.PostId,
                PostTitle = s.Post?.Title,
                s.CreatedAt,
                s.UpdatedAt
            }));
        }

        [HttpGet("history/{sessionId:int}")]
        [Authorize]
        public async Task<IActionResult> GetSessionMessages(int sessionId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var messages = await _chatHistory.GetSessionMessagesAsync(sessionId, userId);
            return Ok(messages);
        }

        [HttpPost("credits")]
        [AllowAnonymous]
        public IActionResult AddCredits([FromBody] AddAiCreditsRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                return BadRequest(new { message = "DeviceId required" });

            int[] allowedCounts = { 5, 10 };
            if (!allowedCounts.Contains(request.Count))
                return BadRequest(new { message = "Invalid credit count" });

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (request.ValidUntilUtc > now + 25 * 3600 || request.ValidUntilUtc < now)
                return BadRequest(new { message = "Invalid expiry" });

            var secret = _configuration["OpenAI:CreditsSigningSecret"] ?? string.Empty;
            if (!string.IsNullOrEmpty(secret))
            {
                var payload = $"{request.DeviceId}:{request.Count}:{request.ValidUntilUtc}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
                if (!string.Equals(expected, request.Signature, StringComparison.OrdinalIgnoreCase))
                    return StatusCode(403, new { message = "Invalid signature" });
            }

            var rateKey = $"device:{request.DeviceId}";
            var validUntil = DateTimeOffset.FromUnixTimeSeconds(request.ValidUntilUtc).UtcDateTime;
            _rateLimitService.AddBonusQuestions(rateKey, request.Count, validUntil);

            var clientId = "tobiso-android";
            var confVal = _configuration[$"OpenAI:ClientLimits:{clientId}"];
            var baseLimit = !string.IsNullOrEmpty(confVal) && int.TryParse(confVal, out var cl) ? cl
                : int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            var effectiveLimit = baseLimit + _rateLimitService.GetBonusTotal(rateKey);

            return Ok(new AddAiCreditsResponse
            {
                Success = true,
                TotalRemainingToday = _rateLimitService.GetRemaining(rateKey, effectiveLimit)
            });
        }

        [HttpPost("ask-stream")]
        [AllowAnonymous]
        public async Task AskStream([FromBody] AiChatRequest request)
        {
            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
            {
                Response.StatusCode = 429;
                await Response.WriteAsync("data: {\"error\":\"Daily limit reached\"}\n\n");
                return;
            }

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers["Connection"] = "keep-alive";

            try
            {
                await foreach (var chunk in _aiService.AskStreamAsync(request))
                {
                    var escaped = chunk.Replace("\n", "\\n").Replace("\r", "");
                    await Response.WriteAsync($"data: {escaped}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Streaming failed for PostId={PostId}", request.PostId);
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }

        [HttpPost("explain-sentence")]
        [AllowAnonymous]
        public async Task<IActionResult> ExplainSentence([FromBody] ExplainSentenceRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Sentence))
                return BadRequest("Missing sentence");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            var post = await _postService.GetById(request.PostId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;

            try
            {
                var explanation = await _aiService.ExplainSentenceAsync(request.Sentence, versionContent);
                return Ok(new ExplainSentenceResponse { Explanation = explanation });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Explain-sentence failed");
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("evaluate-answer")]
        [AllowAnonymous]
        public async Task<IActionResult> EvaluateAnswer([FromBody] EvaluateAnswerRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StudentAnswer))
                return BadRequest("Missing student answer");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.EvaluateAnswerAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Evaluate-answer failed");
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("flashcards")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateFlashcards([FromBody] FlashcardRequest request)
        {
            if (request == null || request.PostId <= 0)
                return BadRequest("Missing postId");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.GenerateFlashcardsAsync(request.PostId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Flashcard generation failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("practice-problems")]
        [AllowAnonymous]
        public async Task<IActionResult> GeneratePracticeProblems([FromBody] PracticeProblemRequest request)
        {
            if (request == null || request.PostId <= 0)
                return BadRequest("Missing postId");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.GeneratePracticeProblemsAsync(request.PostId, request.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Practice problem generation failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("rewrite-grade")]
        [AllowAnonymous]
        public async Task<IActionResult> RewriteForGrade([FromBody] RewriteGradeRequest request)
        {
            if (request == null || request.PostId <= 0)
                return BadRequest("Missing postId");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.RewriteForGradeAsync(request.PostId, request.TargetGrade);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Grade rewrite failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpGet("real-world/{postId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRealWorldApplications(int postId)
        {
            if (postId <= 0) return BadRequest("Invalid postId");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.GetRealWorldApplicationsAsync(postId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Real-world applications failed for PostId={PostId}", postId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpGet("suggest-related/{postId:int}")]
        [Authorize]
        public async Task<IActionResult> SuggestRelatedPosts(int postId)
        {
            if (postId <= 0) return BadRequest("Invalid postId");

            try
            {
                var result = await _aiService.SuggestRelatedPostsAsync(postId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Suggest related failed for PostId={PostId}", postId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        private string GetRateKey()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (Request.Headers.TryGetValue("X-Device-Id", out var dv))
            {
                var deviceId = dv.FirstOrDefault();
                if (!string.IsNullOrEmpty(deviceId)) return $"device:{deviceId}";
            }
            return ip;
        }

        private bool TryConsumeRateLimit(string rateKey)
        {
            var baseLimit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            return _rateLimitService.TryConsume(rateKey, baseLimit + _rateLimitService.GetBonusTotal(rateKey));
        }

        [HttpGet("detect-persons/{postId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DetectPersons(int postId)
        {
            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            var post = await _postService.GetById(postId);
            if (post == null) return NotFound();
            // Choose the most appropriate version: prefer highest grade-level if available, else first.
            // GradeId is non-nullable int — order directly; highest grade = most advanced content.
            var versionContent = post.Versions?.OrderByDescending(v => v.GradeId)
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

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

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

        [HttpPost("generate-question")]
        [Authorize]
        public async Task<IActionResult> GenerateQuestion([FromBody] GenerateQuestionRequest request)
        {
            if (request == null)
                return BadRequest("Missing request body.");

            string content;
            if (request.PostId > 0)
            {
                var post = await _postService.GetById(request.PostId);
                if (post == null) return NotFound();
                content = post.Versions?
                    .OrderByDescending(v => v.GradeLevel ?? int.MinValue)
                    .FirstOrDefault()?.Content ?? string.Empty;
                if (string.IsNullOrWhiteSpace(content))
                    return BadRequest("Post has no content.");
            }
            else if (!string.IsNullOrWhiteSpace(request.Content))
            {
                content = request.Content;
            }
            else
            {
                return BadRequest("Either PostId or Content must be provided.");
            }

            try
            {
                var count = request.Count > 0 ? request.Count : 1;
                var results = await _aiService.GenerateQuestionsAsync(content, count, request.ExistingQuestions ?? new List<string>());
                foreach (var r in results)
                    r.PostId = request.PostId;
                return Ok(results);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Question generation failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpGet("person")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPerson([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Missing name");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

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

        [HttpPost("what-if")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWhatIfScenario([FromBody] WhatIfRequest request)
        {
            if (request == null || request.PostId <= 0) return BadRequest("Invalid request");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.GetWhatIfScenarioAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "What-if scenario failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("evaluate-comprehension")]
        [AllowAnonymous]
        public async Task<IActionResult> EvaluateComprehension([FromBody] EvaluateComprehensionRequest request)
        {
            if (request == null || request.PostId <= 0) return BadRequest("Invalid request");
            if (string.IsNullOrWhiteSpace(request.StudentExplanation)) return BadRequest("Explanation is required");

            var rateKey = GetRateKey();
            if (!TryConsumeRateLimit(rateKey))
                return StatusCode(429, new { message = "Daily limit reached" });

            try
            {
                var result = await _aiService.EvaluateComprehensionAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Comprehension evaluation failed for PostId={PostId}", request.PostId);
                return StatusCode(502, new { message = ex.Message });
            }
        }
    }
}
