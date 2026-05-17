using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.Interfaces;

namespace Tobiso.Web.App.Services
{
    public class AiService : IAiService, Tobiso.Web.Shared.Interfaces.IAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPostService _postService;
        private readonly IAiRateLimitService _rateLimitService;

        public AiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IPostService postService, IAiRateLimitService rateLimitService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _postService = postService;
            _rateLimitService = rateLimitService;
        }

        private string PrepareArticleContext(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            // remove intro block delimited by lines with three dots
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\.\.\.\s*\r?\n[\s\S]*?\r?\n\.\.\.\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

            // remove markdown images/links
            content = System.Text.RegularExpressions.Regex.Replace(content, @"!?\[.*?\]\(.*?\)", string.Empty);
            // remove html img tags
            content = System.Text.RegularExpressions.Regex.Replace(content, "<img[^>]*>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Optionally trim length to avoid sending huge contexts
            if (content.Length > 20000) content = content.Substring(0, 20000);

            return content;
        }

        public async Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var systemPrompt = _configuration["OpenAI:SystemPrompt"] ?? "Jsi AI asistent vzdělávacího webu Tobiso.com. Pomáháš studentům pochopit učivo – vysvětluješ pojmy, uvádíš příklady s řešením. Odpovídáš v češtině stručně a srozumitelně. Pokud otázka přesahuje téma článku nebo si nejsi jistý, odpoviš: 'Nevím.' Nespekuluj.";

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(request.PostId);
            var articleContext = PrepareArticleContext(post?.Content ?? string.Empty);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = $"Article context:\n{articleContext}" }
            };

            if (request.ConversationHistory != null)
            {
                foreach (var m in request.ConversationHistory)
                {
                    messages.Add(new { role = m.Role, content = m.Content });
                }
            }

            messages.Add(new { role = "user", content = request.Question });

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 800
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI request failed for PostId={PostId}", request.PostId);
                throw;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            var contentText = string.Empty;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var messageEl) && messageEl.TryGetProperty("content", out var contentEl))
                {
                    contentText = contentEl.GetString() ?? string.Empty;
                }
                else if (first.TryGetProperty("delta", out var deltaEl) && deltaEl.TryGetProperty("content", out var deltaContent))
                {
                    contentText = deltaContent.GetString() ?? string.Empty;
                }
            }

            var limit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            var remaining = _rateLimitService.GetRemaining(clientKey, limit);

            return new AiChatResponse { Answer = contentText.Trim(), RemainingQuestions = remaining };
        }

        public async Task<string> AskRawJsonAsync(string systemPrompt, string userPrompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            };

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 500,
                response_format = new { type = "json_object" }
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI AskRawJsonAsync request failed");
                throw;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices)
                && choices.GetArrayLength() > 0
                && choices[0].TryGetProperty("message", out var msg)
                && msg.TryGetProperty("content", out var content))
            {
                return content.GetString()?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        public async Task<List<string>> DetectPeopleInTextAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return new List<string>();

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var systemPrompt = _configuration["OpenAI:SystemPrompt"] ?? "You are an assistant that extracts lists of real people mentioned in a text. Return only a JSON array of distinct person names, no extras.";

            var trimmed = PrepareArticleContext(content);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Extract all real person full names mentioned in the following text. Return a JSON array of names only.\n\nText:\n{trimmed}" }
            };

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 400
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI detection request failed");
                return new List<string>();
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                string contentText = string.Empty;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var messageEl) && messageEl.TryGetProperty("content", out var contentEl))
                        contentText = contentEl.GetString() ?? string.Empty;
                }

                // Try to parse any JSON array found inside the returned text
                var names = new List<string>();
                // Find first '[' and ']' and attempt to parse
                var start = contentText.IndexOf('[');
                var end = contentText.LastIndexOf(']');
                if (start >= 0 && end > start)
                {
                    var arr = contentText.Substring(start, end - start + 1);
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<string>>(arr);
                        if (parsed != null)
                        {
                            names.AddRange(parsed.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));
                        }
                    }
                    catch { }
                }

                // Fallback: if no JSON found, try newline-split heuristics
                if (names.Count == 0)
                {
                    var lines = contentText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim()).Where(l => l.Length > 2).ToList();
                    foreach (var l in lines)
                    {
                        // simple heuristic: skip sentences; take short lines
                        if (l.Length < 120 && l.Count(c => char.IsWhiteSpace(c)) >= 1)
                            names.Add(l);
                    }
                }

                // Deduplicate while preserving order
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var result = new List<string>();
                foreach (var n in names)
                {
                    if (string.IsNullOrWhiteSpace(n)) continue;
                    var clean = System.Text.RegularExpressions.Regex.Replace(n, @"[""']", "").Trim();
                    if (!seen.Contains(clean)) { seen.Add(clean); result.Add(clean); }
                }

                return result;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse OpenAI detection response");
                return new List<string>();
            }
        }

        public async Task<GrammarCheckResponse> CheckGrammarAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return new GrammarCheckResponse();

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var systemPrompt = _configuration["OpenAI:GrammarSystemPrompt"] ??
                "You are a multilingual grammar checker. Analyze the provided text and identify grammar, spelling, and punctuation errors. " +
                "Return ONLY a JSON object with a single key \"issues\" containing an array of objects. Each object must have: \"originalText\" (exact incorrect text), \"correction\" (the corrected replacement), \"explanation\" (brief explanation in the same language). " +
                "Focus on clear errors only, not stylistic choices. If no errors are found, return {\"issues\": []}.";

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            // Keep the content reasonably sized
            var trimmed = PrepareArticleContext(content);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Return JSON as described. Text:\n{trimmed}" }
            };

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 800,
                response_format = new { type = "json_object" }
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI grammar check request failed");
                return new GrammarCheckResponse();
            }

            var body = await response.Content.ReadAsStringAsync();
            try
            {
                // Parse top-level JSON object from model output
                // The model may wrap JSON in text, so try to extract first '{'.. '}' span
                var start = body.IndexOf('{');
                var end = body.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    var jsonObj = body.Substring(start, end - start + 1);
                    var doc = JsonDocument.Parse(jsonObj);
                    if (doc.RootElement.TryGetProperty("issues", out var issuesEl) && issuesEl.ValueKind == JsonValueKind.Array)
                    {
                        var issues = new List<GrammarIssue>();
                        foreach (var it in issuesEl.EnumerateArray())
                        {
                            var original = it.TryGetProperty("originalText", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetString() ?? string.Empty : string.Empty;
                            var correction = it.TryGetProperty("correction", out var c) && c.ValueKind != JsonValueKind.Null ? c.GetString() ?? string.Empty : string.Empty;
                            var explanation = it.TryGetProperty("explanation", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrWhiteSpace(original))
                                issues.Add(new GrammarIssue { OriginalText = original, Correction = correction, Explanation = explanation });
                        }

                        return new GrammarCheckResponse { Issues = issues };
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse grammar check response: {Body}", body);
            }

            return new GrammarCheckResponse();
        }
    }
}
