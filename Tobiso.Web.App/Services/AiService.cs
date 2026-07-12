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
            var baseSystemPrompt = _configuration["OpenAI:SystemPrompt"] ?? "Jsi AI asistent vzdělávacího webu Tobiso.com. Pomáháš studentům pochopit učivo – vysvětluješ pojmy, uvádíš příklady s řešením. Odpovídáš v češtině stručně a srozumitelně. Pokud otázka přesahuje téma článku nebo si nejsi jistý, odpoviš: 'Nevím.' Nespekuluj.";
            var systemPrompt = request.SocraticMode
                ? baseSystemPrompt + " SOKRATOVSKÝ MODUS: Nikdy neodpovídej přímo. Místo toho pokládej naváděcí otázky, které studenta přivedou k odpovědi. Odpověz jednou nebo dvěma krátkými otázkami."
                : baseSystemPrompt;

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(request.PostId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);

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
            Serilog.Log.Debug("Grammar check raw response: {Body}", body);
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

        public async Task<string> GenerateCheatSheetAsync(string title, string content, string ratio = "1x1")
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            // Section/bullet counts and token budget per ratio
            // 1x* = 10 cm wide, 2x* = 18 cm wide; x1 = compact, x2 = full content
            var (sectionRange, bulletRange, sizeSuffix, maxTokens) = ratio switch
            {
                "2x1" => ("3–5", "4–7",  "18×10 cm", 700),
                "2x2" => ("4–6", "5–10", "18×20 cm", 1300),
                "1x2" => ("3–5", "5–8",  "10×20 cm", 900),
                _     => ("2–4", "3–5",  "10×10 cm", 550),  // 1x1 default
            };

            var systemPrompt = _configuration["OpenAI:CheatSheetSystemPrompt"] is { Length: > 0 } sp
                ? sp
                : $"Jsi asistent pro tvorbu tahákú. Dostaneš vzdělávací článek a vytvoříš strukturovaný tahák. " +
                  $"Formát (PŘESNĚ dodržuj):\n" +
                  $"### Název sekce\n" +
                  $"• krátký bod (max 8 slov)\n" +
                  $"• krátký bod\n\n" +
                  $"Pravidla: Vytvoř {sectionRange} tematických sekcí. Každá sekce má {bulletRange} bodů. " +
                  $"Žádný úvod ani závěr – POUZE sekce a body. " +
                  $"Vzorce piš textově (a/b, a^2). Odpovídej výhradně v češtině. " +
                  $"Tahák musí být čitelný na ploše {sizeSuffix}.";

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var trimmed = PrepareArticleContext(content);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma: {title}\n\nObsah článku:\n{trimmed}" }
            };

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = maxTokens,
                temperature = 0.3
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI cheat sheet request failed for title={Title}", title);
                throw;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var cnt))
                    return cnt.GetString()?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        public async Task<List<CreateQuestionRequest>> GenerateQuestionsAsync(string content, int count, List<string> existingQuestions)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Content is empty.");

            count = Math.Max(1, Math.Min(count, 10));

            var avoidSection = existingQuestions.Count > 0
                ? $"- Negeneruj otázky podobné těmto již existujícím:\n{string.Join("\n", existingQuestions.Select(q => $"  • {q}"))}\n"
                : string.Empty;

            var systemPrompt =
                $"Jsi tvůrce testových otázek pro český vzdělávací web Tobiso.cz. Na základě obsahu článku vygeneruj PŘESNĚ {count} různých otázek v češtině.\n\n" +
                "Pro každou otázku zvol JEDEN z těchto typů:\n" +
                "- FACTUAL (faktická): jednoznačná odpověď (jméno, datum, vzorec, číslo) → PŘESNĚ 1 odpověď s correct=1\n" +
                "- SINGLE (jedna správná): koncepční otázka → 3–4 odpovědi, PŘESNĚ 1 s correct=1, ostatní correct=0\n" +
                "- MULTI (více správných): otázka kde platí více tvrzení → 3–5 odpovědí, 2–3 s correct=1, ostatní correct=0\n\n" +
                "Další pravidla:\n" +
                "- Každá odpověď max. 15 slov.\n" +
                "- Vysvětlení: přesně 1 prvek, max. 3 věty, vysvětluje správné odpovědi.\n" +
                "- Otázky musí pokrývat různá témata z textu.\n" +
                avoidSection +
                "\nVrať POUZE platný JSON objekt (bez markdown, bez komentářů):\n" +
                "{\"questions\":[{\"questionText\":\"...\",\"answers\":[{\"answerText\":\"...\",\"correct\":1}],\"explanations\":[{\"text\":\"...\"}]}]}";

            var userPrompt = $"Obsah článku:\n{PrepareArticleContext(content)}";

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            };

            var maxTokens = count * 400 + 200;

            var payload = new
            {
                model,
                messages,
                max_tokens = maxTokens,
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
                Serilog.Log.Error(ex, "OpenAI question generation request failed");
                throw;
            }

            var raw = string.Empty;
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    raw = contentEl.GetString()?.Trim() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to read OpenAI question generation response");
                throw;
            }

            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("AI returned an empty response.");

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var rootEl = doc.RootElement;

                JsonElement questionsEl;
                if (!rootEl.TryGetProperty("questions", out questionsEl) || questionsEl.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException("AI response missing 'questions' array.");

                var result = new List<CreateQuestionRequest>();
                foreach (var q in questionsEl.EnumerateArray())
                {
                    var questionText = q.TryGetProperty("questionText", out var qtEl) ? qtEl.GetString() ?? string.Empty : string.Empty;
                    if (string.IsNullOrWhiteSpace(questionText)) continue;

                    var answers = new List<CreateAnswerRequest>();
                    if (q.TryGetProperty("answers", out var answersEl) && answersEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var a in answersEl.EnumerateArray())
                        {
                            var text = a.TryGetProperty("answerText", out var at) ? at.GetString() ?? string.Empty : string.Empty;
                            var correct = a.TryGetProperty("correct", out var cv) && cv.ValueKind == JsonValueKind.Number ? cv.GetInt32() : 0;
                            if (!string.IsNullOrWhiteSpace(text))
                                answers.Add(new CreateAnswerRequest { AnswerText = text, Correct = correct });
                        }
                    }
                    if (answers.Count == 0) continue;

                    var explanations = new List<CreateExplanationRequest>();
                    if (q.TryGetProperty("explanations", out var explEl) && explEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var e in explEl.EnumerateArray())
                        {
                            var text = e.TryGetProperty("text", out var et) ? et.GetString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrWhiteSpace(text))
                                explanations.Add(new CreateExplanationRequest { Text = text });
                        }
                    }

                    result.Add(new CreateQuestionRequest { QuestionText = questionText, Answers = answers, Explanations = explanations });
                }

                if (result.Count == 0)
                    throw new InvalidOperationException("AI response contained no valid questions.");

                return result;
            }
            catch (JsonException ex)
            {
                Serilog.Log.Error(ex, "Failed to parse AI question generation response. Raw: {Raw}", raw);
                throw new InvalidOperationException("AI response was not valid JSON.", ex);
            }
        }

        public async IAsyncEnumerable<string> AskStreamAsync(AiChatRequest request)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var systemPrompt = _configuration["OpenAI:SystemPrompt"] ?? "Jsi AI asistent vzdělávacího webu Tobiso.com. Pomáháš studentům pochopit učivo – vysvětluješ pojmy, uvádíš příklady s řešením. Odpovídáš v češtině stručně a srozumitelně. Pokud otázka přesahuje téma článku nebo si nejsi jistý, odpoviš: 'Nevím.' Nespekuluj.";

            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(request.PostId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = $"Article context:\n{articleContext}" }
            };

            if (request.ConversationHistory != null)
            {
                foreach (var m in request.ConversationHistory)
                    messages.Add(new { role = m.Role, content = m.Content });
            }

            messages.Add(new { role = "user", content = request.Question });

            var payload = new { model, messages, max_tokens = 800, stream = true };

            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI stream request failed for PostId={PostId}", request.PostId);
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                string? delta = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var docRoot = doc.RootElement;
                    if (docRoot.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var deltaEl) && deltaEl.TryGetProperty("content", out var contentEl))
                            delta = contentEl.GetString();
                    }
                }
                catch { }

                if (delta != null)
                    yield return delta;
            }
        }

        public async Task<string> ExplainSentenceAsync(string sentence, string articleContext)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return string.Empty;

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var systemPrompt = "Jsi výukový asistent. Dostaneš větu z článku a kontext článku. Vysvětli smysl věty jednoduše, v 1–2 větách, česky. Odpovídej jen vysvětlením, bez úvodu.";
            var userPrompt = string.IsNullOrWhiteSpace(articleContext)
                ? $"Věta: {sentence}"
                : $"Kontext článku (úryvek):\n{articleContext.Substring(0, Math.Min(articleContext.Length, 3000))}\n\nVěta: {sentence}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            };

            var payload = new { model, messages, max_tokens = 200, temperature = 0 };
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
                Serilog.Log.Error(ex, "OpenAI explain-sentence request failed");
                return string.Empty;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var cnt))
                    return cnt.GetString()?.Trim() ?? string.Empty;
            }
            return string.Empty;
        }

        public async Task<EvaluateAnswerResponse> EvaluateAnswerAsync(EvaluateAnswerRequest request)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var systemPrompt = "Jsi výukový asistent. Porovnej studentovu odpověď se správnou odpovědí na danou otázku. Vrať JSON: {\"correct\":true nebo false,\"feedback\":\"stručná zpětná vazba max 2 věty, česky\"}. Buď laskavý a povzbudivý.";
            var userPrompt = $"Otázka: {request.QuestionText}\nSprávná odpověď: {request.CorrectAnswer}\nStudentova odpověď: {request.StudentAnswer}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            };

            var payload = new { model, messages, max_tokens = 150, temperature = 0, response_format = new { type = "json_object" } };
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
                Serilog.Log.Error(ex, "OpenAI evaluate-answer request failed");
                return new EvaluateAnswerResponse { IsCorrect = false, Feedback = "Hodnocení momentálně nedostupné." };
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    var inner = innerDoc.RootElement;
                    var isCorrect = inner.TryGetProperty("correct", out var c) && c.ValueKind == JsonValueKind.True;
                    var feedback = inner.TryGetProperty("feedback", out var f) ? f.GetString() ?? string.Empty : string.Empty;
                    return new EvaluateAnswerResponse { IsCorrect = isCorrect, Feedback = feedback };
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse evaluate-answer response");
            }

            return new EvaluateAnswerResponse { IsCorrect = false, Feedback = "Hodnocení momentálně nedostupné." };
        }

        public async Task<FlashcardResponse> GenerateFlashcardsAsync(int postId)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext))
                return new FlashcardResponse();

            var systemPrompt = "Jsi tvůrce výukových kartiček. Ze vzdělávacího textu extrahuj 10–15 párů pojem/definice. Každý pojem max 5 slov, každá definice max 20 slov. Vrať POUZE platný JSON objekt: {\"cards\":[{\"term\":\"...\",\"definition\":\"...\"}]}. Odpovídej v češtině.";
            var userPrompt = $"Téma: {title}\n\nObsah článku:\n{articleContext}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            };

            var payload = new { model, messages, max_tokens = 1200, temperature = 0.2, response_format = new { type = "json_object" } };
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
                Serilog.Log.Error(ex, "OpenAI flashcard request failed for postId={PostId}", postId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    var inner = innerDoc.RootElement;
                    if (inner.TryGetProperty("cards", out var cardsEl) && cardsEl.ValueKind == JsonValueKind.Array)
                    {
                        var cards = new List<FlashcardCard>();
                        foreach (var card in cardsEl.EnumerateArray())
                        {
                            var term = card.TryGetProperty("term", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                            var definition = card.TryGetProperty("definition", out var d) ? d.GetString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrWhiteSpace(term))
                                cards.Add(new FlashcardCard { Term = term, Definition = definition });
                        }
                        return new FlashcardResponse { Cards = cards };
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse flashcard response for postId={PostId}", postId);
                throw;
            }

            return new FlashcardResponse();
        }

        public async Task<PracticeProblemResponse> GeneratePracticeProblemsAsync(int postId, int count)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            count = Math.Clamp(count, 1, 10);
            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new PracticeProblemResponse();

            var systemPrompt =
                $"Jsi tvůrce cvičných úloh pro výuku. Na základě obsahu článku vygeneruj PŘESNĚ {count} cvičných úloh. " +
                "Pro každou úlohu urči obtížnost (lehká/střední/těžká) a napiš podrobné řešení krok za krokem. " +
                "Úlohy musí být výpočetní nebo analytické – nevytvářej jen faktické otázky. " +
                "Vrať POUZE platný JSON (bez markdown): {\"problems\":[{\"problemText\":\"...\",\"solution\":\"...\",\"difficulty\":\"lehká|střední|těžká\"}]}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma: {title}\n\nObsah článku:\n{articleContext}" }
            };

            var payload = new { model, messages, max_tokens = count * 400 + 200, temperature = 0.5, response_format = new { type = "json_object" } };
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
                Serilog.Log.Error(ex, "OpenAI practice-problems request failed for postId={PostId}", postId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    var inner = innerDoc.RootElement;
                    if (inner.TryGetProperty("problems", out var problemsEl) && problemsEl.ValueKind == JsonValueKind.Array)
                    {
                        var problems = new List<PracticeProblem>();
                        foreach (var p in problemsEl.EnumerateArray())
                        {
                            var problemText = p.TryGetProperty("problemText", out var pt) ? pt.GetString() ?? string.Empty : string.Empty;
                            var solution = p.TryGetProperty("solution", out var sl) ? sl.GetString() ?? string.Empty : string.Empty;
                            var difficulty = p.TryGetProperty("difficulty", out var df) ? df.GetString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrWhiteSpace(problemText))
                                problems.Add(new PracticeProblem { ProblemText = problemText, Solution = solution, Difficulty = difficulty });
                        }
                        return new PracticeProblemResponse { Problems = problems };
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse practice-problems response for postId={PostId}", postId);
                throw;
            }

            return new PracticeProblemResponse();
        }

        public async Task<RewriteGradeResponse> RewriteForGradeAsync(int postId, int targetGrade)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new RewriteGradeResponse();

            var systemPrompt = $"Jsi pedagog. Přepiš následující vzdělávací text tak, aby byl srozumitelný pro žáka {targetGrade}. ročníku základní školy. " +
                "Přizpůsob slovní zásobu, délku vět a hloubku vysvětlení věkové skupině. Zachovej klíčové informace a fakta. " +
                "Odpovídej čistým textem v češtině, bez markdown formátování a bez úvodní věty jako 'Přepsaný text:'.";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma: {title}\n\n{articleContext}" }
            };

            var payload = new { model, messages, max_tokens = 1200, temperature = 0.4 };
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
                Serilog.Log.Error(ex, "OpenAI rewrite-grade request failed for postId={PostId}", postId);
                throw;
            }

            using var respStream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(respStream);
            var docRoot = doc.RootElement;
            if (docRoot.TryGetProperty("choices", out var ch) && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c))
            {
                return new RewriteGradeResponse { Content = c.GetString()?.Trim() ?? string.Empty };
            }

            return new RewriteGradeResponse();
        }

        public async Task<RealWorldResponse> GetRealWorldApplicationsAsync(int postId)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new RealWorldResponse();

            var systemPrompt = "Jsi vzdělávací poradce. Na základě obsahu článku vyjmenuj 3 konkrétní příklady reálného využití daného tématu v každodenním životě nebo v praxi. " +
                "Každý příklad musí být stručný (max 2 věty), konkrétní a motivující pro žáky. " +
                "Vrať POUZE platný JSON (bez markdown): {\"applications\":[\"...\",\"...\",\"...\"]}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma: {title}\n\n{articleContext.Substring(0, Math.Min(articleContext.Length, 4000))}" }
            };

            var payload = new { model, messages, max_tokens = 400, temperature = 0.4, response_format = new { type = "json_object" } };
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
                Serilog.Log.Error(ex, "OpenAI real-world request failed for postId={PostId}", postId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    if (innerDoc.RootElement.TryGetProperty("applications", out var appsEl) && appsEl.ValueKind == JsonValueKind.Array)
                    {
                        var apps = appsEl.EnumerateArray()
                            .Select(a => a.GetString() ?? string.Empty)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
                        return new RealWorldResponse { Applications = apps };
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse real-world response for postId={PostId}", postId);
            }

            return new RealWorldResponse();
        }

        public async Task<SuggestRelatedResponse> SuggestRelatedPostsAsync(int postId)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            var allPosts = await _postService.GetAll();
            var otherPosts = allPosts.Where(p => p.Id != postId).ToList();

            if (!otherPosts.Any()) return new SuggestRelatedResponse();

            var postList = string.Join("\n", otherPosts.Select(p => $"ID:{p.Id} – {p.Title}"));

            var systemPrompt = "You are a content curator for a Czech educational platform. Given a source article and a list of articles, select the 5 IDs that are most conceptually related — by topic overlap, prerequisites, or complementary knowledge. Return ONLY valid JSON: {\"ids\":[1,2,3,4,5]}. Use integer IDs from the list.";
            var userPrompt = $"Source article: \"{title}\"\n\nExcerpt:\n{articleContext.Substring(0, Math.Min(articleContext.Length, 2000))}\n\nAll articles:\n{postList}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            };

            var payload = new { model, messages, max_tokens = 120, temperature = 0, response_format = new { type = "json_object" } };
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
                Serilog.Log.Error(ex, "OpenAI suggest-related request failed for postId={PostId}", postId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    if (innerDoc.RootElement.TryGetProperty("ids", out var idsEl) && idsEl.ValueKind == JsonValueKind.Array)
                    {
                        var ids = idsEl.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.Number)
                            .Select(e => e.GetInt32())
                            .Where(id => otherPosts.Any(p => p.Id == id))
                            .Take(5)
                            .ToList();
                        return new SuggestRelatedResponse { PostIds = ids };
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse suggest-related response for postId={PostId}", postId);
            }

            return new SuggestRelatedResponse();
        }

        public async Task<GrammarCheckResponse> CheckGrammarAsync(string content)        {
            if (string.IsNullOrWhiteSpace(content)) return new GrammarCheckResponse();

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var systemPrompt = _configuration["OpenAI:GrammarSystemPrompt"] ??
                "You are a deterministic multilingual grammar checker. Analyze the provided TEXT and identify only clear grammar, spelling, or punctuation mistakes. Do NOT make stylistic suggestions.\n" +
                "RESPONSE FORMAT: Return ONLY a single valid JSON object with exactly one key: \"issues\", whose value is an array. Each item in the array must be an object with these keys: \"originalText\" (string - the exact incorrect snippet from the TEXT, verbatim), \"correction\" (string - the corrected replacement), \"explanation\" (string - a brief explanation in the SAME LANGUAGE as the TEXT), \"start\" (integer index of first character in the provided TEXT, zero-based) and \"end\" (integer index of the character AFTER the last character - exclusive). If you cannot provide offsets, you may use -1 for start and end, but still include them.\n" +
                "EXAMPLE: {\"issues\": [{\"originalText\": \"vískyt\", \"correction\": \"výskyt\", \"explanation\": \"chybná diakritika\", \"start\": 0, \"end\": 6}]}\n" +
                "If there are no issues, return {\"issues\": []}. DO NOT output any surrounding text, markdown, or commentary.";

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
                temperature = 0,
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
                using var wrapperDoc = JsonDocument.Parse(body);
                var root = wrapperDoc.RootElement;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    if (choice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var contentEl) &&
                        contentEl.ValueKind == JsonValueKind.String)
                    {
                        var contentJson = contentEl.GetString();
                        if (!string.IsNullOrWhiteSpace(contentJson))
                        {
                            using var issuesDoc = JsonDocument.Parse(contentJson);
                            if (issuesDoc.RootElement.TryGetProperty("issues", out var issuesEl) && issuesEl.ValueKind == JsonValueKind.Array)
                            {
                                var issues = new List<GrammarIssue>();
                                foreach (var it in issuesEl.EnumerateArray())
                                {
                                    var original = it.TryGetProperty("originalText", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetString() ?? string.Empty : string.Empty;
                                    var correction = it.TryGetProperty("correction", out var c) && c.ValueKind != JsonValueKind.Null ? c.GetString() ?? string.Empty : string.Empty;
                                    var explanation = it.TryGetProperty("explanation", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() ?? string.Empty : string.Empty;

                                    if (!string.IsNullOrWhiteSpace(original))
                                    {
                                        issues.Add(new GrammarIssue { OriginalText = original, Correction = correction, Explanation = explanation });
                                    }
                                }

                                return new GrammarCheckResponse { Issues = issues };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse grammar check response. Raw: {Body}", body);
            }

            return new GrammarCheckResponse();
        }

        public async Task<WhatIfResponse> GetWhatIfScenarioAsync(WhatIfRequest request)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(request.PostId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new WhatIfResponse { Scenario = "Článek nemá obsah.", Explanation = string.Empty };

            var systemPrompt = "Jsi kreativní vědecký myslitel. Na základě tématu článku odpověz na myšlenkovou otázku 'Co kdyby?' zadanou žákem. " +
                "Odpověď musí být vědecky zajímavá, konkrétní a vhodná pro žáky základní školy. " +
                "Vrať POUZE platný JSON (bez markdown): {\"scenario\":\"[zopakuj nebo přeformuluj otázku žáka]\",\"explanation\":\"[2-3 věty fascinujícího vysvětlení dopadů]\"}";

            var userContent = string.IsNullOrWhiteSpace(request.UserQuestion)
                ? $"Téma článku: {title}\n\n{articleContext.Substring(0, Math.Min(articleContext.Length, 3000))}\n\nVymysli zajímavou otázku 'Co kdyby?' a odpověz na ni."
                : $"Téma článku: {title}\n\n{articleContext.Substring(0, Math.Min(articleContext.Length, 2000))}\n\nOtázka žáka: {request.UserQuestion}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            };

            var payload = new { model, messages, max_tokens = 350, temperature = 0.85, response_format = new { type = "json_object" } };
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI what-if request failed for postId={PostId}", request.PostId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    var scenario = innerDoc.RootElement.TryGetProperty("scenario", out var sc) ? sc.GetString() ?? string.Empty : string.Empty;
                    var explanation = innerDoc.RootElement.TryGetProperty("explanation", out var ex2) ? ex2.GetString() ?? string.Empty : string.Empty;
                    return new WhatIfResponse { Scenario = scenario, Explanation = explanation };
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse what-if response for postId={PostId}", request.PostId);
            }

            return new WhatIfResponse();
        }

        public async Task<EvaluateComprehensionResponse> EvaluateComprehensionAsync(EvaluateComprehensionRequest request)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(request.PostId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new EvaluateComprehensionResponse { Feedback = "Nelze vyhodnotit – článek nemá obsah." };

            var systemPrompt = "Jsi přátelský vzdělávací hodnotitel. Žák se pokusil vlastními slovy vysvětlit obsah článku (Feynmanova metoda). " +
                "Zhodnoť jeho porozumění, ale buď povzbudivý a konstruktivní – žák teprve studuje. " +
                "Vrať POUZE platný JSON (bez markdown): {\"score\":8,\"feedback\":\"[1-2 věty celkového hodnocení]\",\"strongPoints\":[\"...\",\"...\"],\"missingPoints\":[\"...\",\"...\"]}. " +
                "score je celé číslo 0-10. strongPoints a missingPoints jsou max 3 položky každá.";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma článku: {title}\n\nObsah článku:\n{articleContext.Substring(0, Math.Min(articleContext.Length, 4000))}\n\nVysvětlení žáka:\n{request.StudentExplanation}" }
            };

            var payload = new { model, messages, max_tokens = 500, temperature = 0.3, response_format = new { type = "json_object" } };
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "OpenAI comprehension evaluation failed for postId={PostId}", request.PostId);
                throw;
            }

            try
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var wrapperDoc = await JsonDocument.ParseAsync(stream);
                var root = wrapperDoc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var msg)
                    && msg.TryGetProperty("content", out var contentEl))
                {
                    var raw = contentEl.GetString() ?? "{}";
                    using var innerDoc = JsonDocument.Parse(raw);
                    var score = innerDoc.RootElement.TryGetProperty("score", out var sc) ? sc.GetInt32() : 0;
                    var feedback = innerDoc.RootElement.TryGetProperty("feedback", out var fb) ? fb.GetString() ?? string.Empty : string.Empty;
                    var strong = innerDoc.RootElement.TryGetProperty("strongPoints", out var sp) && sp.ValueKind == JsonValueKind.Array
                        ? sp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                        : new List<string>();
                    var missing = innerDoc.RootElement.TryGetProperty("missingPoints", out var mp) && mp.ValueKind == JsonValueKind.Array
                        ? mp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                        : new List<string>();
                    return new EvaluateComprehensionResponse { Score = score, Feedback = feedback, StrongPoints = strong, MissingPoints = missing };
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to parse comprehension response for postId={PostId}", request.PostId);
            }

            return new EvaluateComprehensionResponse();
        }

        public async Task<RewriteGradeResponse> RewriteForRegisterAsync(int postId, string register)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var post = await _postService.GetById(postId);
            var versionContent = post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty;
            var articleContext = PrepareArticleContext(versionContent);
            var title = post?.Title ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleContext)) return new RewriteGradeResponse();

            var systemPrompt = register switch
            {
                "simple" => "Jsi pedagog. Přepiš tento vzdělávací text tak, aby mu rozumělo 8leté dítě. Používej velmi krátké věty, jednoduché slovo, přirovnání z každodenního života (hračky, jídlo, rodina). Vyhni se odborným termínům – pokud musíš, vysvětli je velmi jednoduše. Odpovídej čistým textem v češtině, bez markdown formátování.",
                "expert"  => "Jsi odborník. Přepiš tento vzdělávací text do odborné formy vhodné pro experta v oboru. Používej správnou terminologii, předpokládej hluboké znalosti tématu, přidej kontext a nuance. Odpovídej čistým textem v češtině, bez markdown formátování.",
                _         => "Jsi pedagog. Přepiš tento vzdělávací text pro studenta gymnázia. Používej přesné termíny a buď srozumitelný, ale nevyhýbej se odbornosti. Odpovídej čistým textem v češtině, bez markdown formátování."
            };

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Téma: {title}\n\n{articleContext}" }
            };

            var payload = new { model, messages, max_tokens = 1200, temperature = 0.4 };
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
                Serilog.Log.Error(ex, "OpenAI rewrite-register request failed for postId={PostId}", postId);
                throw;
            }

            using var respStream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(respStream);
            var docRoot = doc.RootElement;
            if (docRoot.TryGetProperty("choices", out var ch) && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c))
            {
                return new RewriteGradeResponse { Content = c.GetString()?.Trim() ?? string.Empty };
            }

            return new RewriteGradeResponse();
        }

        public async Task<List<string>> GenerateFunFactsAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new List<string>();

            var systemPrompt = "Jsi zajímavý pedagog. Vygeneruj 3 překvapivé, zajímavé a méně známé fakty o tématu tohoto článku. Fakty musí být stručné (1-2 věty), vzdělávací a vhodné pro studenty ZŠ/SŠ. Odpověz výhradně jako JSON array of strings, bez dalšího textu.";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var facts = new List<string>();
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var el in root.EnumerateArray())
                        if (el.ValueKind == JsonValueKind.String && el.GetString() is string s)
                            facts.Add(s);
                return facts;
            }
            catch { return new List<string>(); }
        }

        public async Task<List<ExamQuestion>> GenerateExamQuestionsAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new List<ExamQuestion>();

            var systemPrompt = "Jsi zkušený pedagog. Vygeneruj 5 nejpravděpodobnějších zkouškových otázek z tohoto článku, které by mohl položit učitel při testu nebo zkoušení. Pro každou otázku přidej vzornou odpověď. Odpověz výhradně jako JSON array: [{\"question\": \"...\", \"answer\": \"...\"}], bez dalšího textu.";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var questions = new List<ExamQuestion>();
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var el in root.EnumerateArray())
                    {
                        var q = el.TryGetProperty("question", out var qp) ? qp.GetString() ?? "" : "";
                        var a = el.TryGetProperty("answer", out var ap) ? ap.GetString() ?? "" : "";
                        if (!string.IsNullOrWhiteSpace(q))
                            questions.Add(new ExamQuestion { Question = q, Answer = a });
                    }
                return questions;
            }
            catch { return new List<ExamQuestion>(); }
        }

        public async Task<string> ExplainWhyAsync(string sentence, string articleContext)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var systemPrompt = "Jsi pedagog. Vysvětli PROČ je daná věta pravda – zaměř se na příčiny a důvody, ne na definici. Používej analogie a příklady z každodenního života. Odpovídej v češtině, max. 3 věty.";
            var trimmed = PrepareArticleContext(articleContext);
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = $"Kontext článku:\n{trimmed}" },
                new { role = "user", content = $"Proč je pravda: \"{sentence}\"?" }
            };
            var payload = new { model, messages, max_tokens = 300 };
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var ch) && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c))
                return c.GetString()?.Trim() ?? string.Empty;
            return string.Empty;
        }

        public async Task<List<KeyTermEntry>> GenerateKeyTermsAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new List<KeyTermEntry>();

            var systemPrompt = "Jsi pedagog. Z článku vyber 8–12 klíčových odborných pojmů a pro každý napiš stručnou 1-větnou definici. Vrať výhradně jako JSON array: [{\"term\": \"...\", \"definition\": \"...\"}]. Pojmy musí být přesně ve tvaru, v jakém se vyskytují v textu (stejný tvar slova).";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var terms = new List<KeyTermEntry>();
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var el in root.EnumerateArray())
                    {
                        var t = el.TryGetProperty("term", out var tp) ? tp.GetString() ?? "" : "";
                        var d = el.TryGetProperty("definition", out var dp) ? dp.GetString() ?? "" : "";
                        if (!string.IsNullOrWhiteSpace(t))
                            terms.Add(new KeyTermEntry { Term = t, Definition = d });
                    }
                return terms;
            }
            catch { return new List<KeyTermEntry>(); }
        }

        public async Task<string> GenerateComparisonAsync(int postId, string compareTo)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var systemPrompt = "Jsi pedagog. Vytvoř přehlednou srovnávací tabulku v Markdownu (s | oddělovači) mezi dvěma pojmy. Tabulka musí mít záhlaví a alespoň 5 řádků s různými kritérii srovnání. Piš v češtině.";
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = $"Kontext článku o tématu '{title}':\n{content}" },
                new { role = "user", content = $"Porovnej: \"{title}\" vs. \"{compareTo}\"" }
            };
            var payload = new { model, messages, max_tokens = 700 };
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var ch) && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c))
                return c.GetString()?.Trim() ?? string.Empty;
            return string.Empty;
        }

        public async Task<StepSolverResponse> GenerateStepSolverAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new StepSolverResponse();

            var systemPrompt = "Jsi pedagog. Z článku vyber hlavní příklad nebo úlohu a vyřeš ji krok za krokem. Pro každý krok uveď název kroku a jeho vysvětlení. Odpověz výhradně jako JSON array: [{\"step\": \"Krok 1: ...\", \"explanation\": \"...\"}]. Maximálně 7 kroků.";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var steps = new List<SolverStep>();
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var el in root.EnumerateArray())
                    {
                        var s = el.TryGetProperty("step", out var sp) ? sp.GetString() ?? "" : "";
                        var e = el.TryGetProperty("explanation", out var ep) ? ep.GetString() ?? "" : "";
                        if (!string.IsNullOrWhiteSpace(s))
                            steps.Add(new SolverStep { Step = s, Explanation = e });
                    }
                return new StepSolverResponse { Steps = steps };
            }
            catch { return new StepSolverResponse(); }
        }

        public async Task<string> GenerateInteractiveDemoAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            if (string.IsNullOrEmpty(apiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

            var systemPrompt = "Jsi expert na tvorbu interaktivních vzdělávacích demonstrací. Vytvoř SKUTEČNĚ interaktivní HTML/JS aktivitu, která učí nebo procvičuje hlavní myšlenku článku. Vanilla JS, ŽÁDNÉ externí knihovny. Výstup: POUZE kompletní HTML soubor začínající <!DOCTYPE html>. Max 200 řádků kódu.\n\nNEJDŘÍV VYBER SPRÁVNÝ TYP AKTIVITY podle povahy tématu (nedělej u všeho canvas simulaci):\n- Přírodní vědy / matematika / fyzika / chemie (něco, co jde simulovat): interaktivní simulace nebo animovaný diagram na Canvas/SVG se slidery či tlačítky (např. změna parametru mění graf/animaci).\n- Literatura / jazyk / společenské vědy: interaktivní KVÍZ s otázkami a vyhodnocením, přiřazovací hra (spáruj pojem↔definici, autor↔dílo), klikací příklady odhalující vysvětlení, doplňovačka, nebo krátká interaktivní ukázka literárního jevu.\n- Dějepis / vývojová témata: interaktivní ČASOVÁ OSA, kde kliknutím na událost se zobrazí popis, nebo kvíz na pořadí událostí.\n\nPOVINNÉ – SKUTEČNÁ INTERAKCE A UČENÍ: Uživatel musí něco DĚLAT (klikat, přetahovat, odpovídat, posouvat) a dostávat ZPĚTNOU VAZBU (správně/špatně, skóre, odhalené vysvětlení, změna vizualizace). Aktivita musí čerpat z KONKRÉTNÍHO OBSAHU článku (konkrétní pojmy, jména, fakta), ne obecné fráze.\n\nZAKÁZÁNO: NEDĚLEJ pasivní ukázku, která jen zobrazí název článku a jedno tlačítko „Zobrazit více\\\" / „Další téma\\\" bez skutečné aktivity. NEZOBRAZUJ jen barevný obdélník s textem. To NENÍ demo.\n\nSPRÁVNOST KVÍZU (KRITICKÉ – tyto chyby se NESMÍ stát):\n1) Odpovědi MUSÍ logicky odpovídat otázce. Např. na otázku „Z jakého jazyka pochází název fejeton?\\\" jsou odpověďmi JAZYKY (francouzština, latina, němčina…), NIKDY jména autorů. Zkontroluj u každé otázky, že všechny nabízené odpovědi jsou smysluplné odpovědi PRÁVĚ na tuto otázku.\n2) Každá otázka má SVOJI vlastní sadu odpovědí a index správné odpovědi. Ulož data jako pole objektů, např. [{q:\\\"...\\\", options:[\\\"...\\\"], correct:0}]. Při přechodu na další otázku VŽDY znovu vykresli JAK text otázky, TAK VŠECHNY tlačítka odpovědí z aktuálního objektu otázky – nikdy nenechávej staré odpovědi z předchozí otázky. (Častá chyba: aktualizuje se jen text otázky, ale tlačítka odpovědí zůstanou stará – to je ZAKÁZÁNO.)\n3) Právě jedna odpověď je správná a musí být fakticky pravdivá podle obsahu článku. Po odpovědi dej zpětnou vazbu (správně/špatně) a umožni pokračovat na další otázku.\n\nBAREVNÁ PALETA (POVINNÁ – použij PŘESNĚ tyto barvy, žádnou modrou): světlé pozadí #fdf3f9, text #3a2530, hlavní akcent #d175a6, tmavší akcent #c36f9a, doplňkový akcent #d89dbd, jemné pozadí prvků #f3d6e5. Tlačítka/ovládací prvky používají akcent #c36f9a s bílým textem, hover #903c67.\n\nROZVRŽENÍ A VELIKOSTI (KRITICKÉ – demo je vloženo v úzkém sloupci ~320–360 px): html,body { margin:0; padding:10px; box-sizing:border-box; width:100%; max-width:100%; overflow-x:hidden; } vše používej box-sizing:border-box. Používej relativní jednotky (%, rem) místo pevných px pro rozměry. ŽÁDNÝ prvek nesmí být širší než kontejner – nikdy nenastavuj pevnou šířku v px větší než ~300 px. Canvas/SVG: šířka 100 %, max-width:100 %, výška úměrná. U SVG VŽDY používej atribut viewBox (např. viewBox=\\\"0 0 300 200\\\") + width=\\\"100%\\\" + preserveAspectRatio, aby se obsah i text zmenšil dovnitř. U Canvasu nastav vnitřní rozlišení podle element.clientWidth a překresli. NIKDY nesmí obsah přetéct do stran (žádný horizontální posuvník, žádný oříznutý text). Text VŽDY zalamuj (overflow-wrap:break-word) – nesmí být oříznutý na okraji. Ovládací prvky (slidery, tlačítka) umísti pod plátno, plně viditelné.\n\nVZHLED: Zaoblené rohy u boxů, tlačítek i plátna (border-radius: 10px). Jemné okraje. Font sans-serif, čitelné velikosti.\n\nKONTRAST (POVINNÝ – text musí být vždy dobře čitelný): Na světlém pozadí (#fdf3f9, #f3d6e5) používej TMAVÝ text (#3a2530). Barevné/zvýrazněné plochy a aktivní tlačítka dělej v sytém akcentu #c36f9a s BÍLÝM textem (#ffffff). NEPOUŽÍVEJ tmavý text na středně sytém růžovém pozadí (#d89dbd) – to má špatný kontrast. Funkční bez jakýchkoliv externích závislostí.";
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Vytvoř interaktivní demo pro téma: {title}\n\nObsah článku:\n{content.Substring(0, Math.Min(content.Length, 3000))}" }
            };
            var payload = new { model, messages, max_tokens = 2000 };
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var ch) && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c))
            {
                var html = c.GetString()?.Trim() ?? string.Empty;
                // Strip markdown code fences if present
                if (html.StartsWith("```"))
                {
                    var firstNewline = html.IndexOf('\n');
                    if (firstNewline > 0) html = html[(firstNewline + 1)..];
                    if (html.EndsWith("```")) html = html[..^3].TrimEnd();
                }
                return InjectDemoSafetyStyles(html);
            }
            return string.Empty;
        }

        // Guarantees the generated demo can never overflow the narrow card it lives in,
        // regardless of what the model produced (fixed widths, un-wrapped text, oversized canvas).
        private static string InjectDemoSafetyStyles(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;
            const string style = "<style>"
                + "html,body{margin:0;padding:10px;box-sizing:border-box;width:100%;max-width:100%;overflow-x:hidden;font-family:system-ui,-apple-system,'Segoe UI',sans-serif;}"
                + "*,*::before,*::after{box-sizing:border-box;}"
                + "body,div,p,span,h1,h2,h3,h4,button,label,li{overflow-wrap:break-word;word-break:break-word;}"
                + "svg,canvas,img,video,table{max-width:100%!important;height:auto;}"
                + "canvas{display:block;}"
                + "</style>";

            var headClose = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headClose >= 0) return html.Insert(headClose, style);

            var bodyOpen = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
            if (bodyOpen >= 0)
            {
                var bodyTagEnd = html.IndexOf('>', bodyOpen);
                if (bodyTagEnd >= 0) return html.Insert(bodyTagEnd + 1, style);
            }
            return style + html;
        }

        public async Task<ConceptMapResponse> GenerateConceptMapAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new ConceptMapResponse();

            var systemPrompt = "Jsi pedagog. Z článku extrahuj 6–12 klíčových pojmů a jejich vzájemné vztahy jako myšlenkovou mapu. Vrať výhradně jako JSON objekt: {\"nodes\": [{\"id\": \"n1\", \"label\": \"Pojem\"}], \"edges\": [{\"source\": \"n1\", \"target\": \"n2\", \"label\": \"je součástí\"}]}. Bez dalšího textu.";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var nodes = new List<ConceptNode>();
                var edges = new List<ConceptEdge>();
                if (root.TryGetProperty("nodes", out var nodesEl))
                    foreach (var n in nodesEl.EnumerateArray())
                        nodes.Add(new ConceptNode
                        {
                            Id = n.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                            Label = n.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : ""
                        });
                if (root.TryGetProperty("edges", out var edgesEl))
                    foreach (var e in edgesEl.EnumerateArray())
                        edges.Add(new ConceptEdge
                        {
                            Source = e.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "",
                            Target = e.TryGetProperty("target", out var tgt) ? tgt.GetString() ?? "" : "",
                            Label = e.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : ""
                        });
                return new ConceptMapResponse { Nodes = nodes, Edges = edges };
            }
            catch { return new ConceptMapResponse(); }
        }

        public async Task<FormulaVarsResponse> ExtractFormulaVarsAsync(int postId)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new FormulaVarsResponse();

            var systemPrompt = "Jsi fyzik/matematik. Z článku extrahuj matematické/fyzikální vzorce vhodné pro interaktivní playground se slidery. Pro každý vzorec uveď: formula (LaTeX), expression (JavaScript výraz, např. 'm * a'), resultVar (proměnná výsledku), resultUnit (jednotka), variables (pole proměnných). Vrať výhradně JSON: {\"formulas\": [{\"formula\": \"F = m \\\\cdot a\", \"expression\": \"m * a\", \"resultVar\": \"F\", \"resultUnit\": \"N\", \"variables\": [{\"name\": \"m\", \"label\": \"Hmotnost\", \"unit\": \"kg\", \"min\": 1, \"max\": 100, \"defaultVal\": 10, \"step\": 1}, {\"name\": \"a\", \"label\": \"Zrychlení\", \"unit\": \"m/s²\", \"min\": 0, \"max\": 50, \"defaultVal\": 9.8, \"step\": 0.1}]}]}. Pokud článek neobsahuje vzorce, vrať {\"formulas\": []}.";
            var userPrompt = $"Téma: {title}\n\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var result = new FormulaVarsResponse();
                if (!root.TryGetProperty("formulas", out var formulasEl)) return result;
                foreach (var f in formulasEl.EnumerateArray())
                {
                    var entry = new FormulaEntry
                    {
                        Formula = f.TryGetProperty("formula", out var fp) ? fp.GetString() ?? "" : "",
                        Expression = f.TryGetProperty("expression", out var ep) ? ep.GetString() ?? "" : "",
                        ResultVar = f.TryGetProperty("resultVar", out var rvp) ? rvp.GetString() ?? "" : "",
                        ResultUnit = f.TryGetProperty("resultUnit", out var rup) ? rup.GetString() ?? "" : ""
                    };
                    if (f.TryGetProperty("variables", out var varsEl))
                        foreach (var v in varsEl.EnumerateArray())
                            entry.Variables.Add(new FormulaVariable
                            {
                                Name = v.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                Label = v.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                                Unit = v.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "",
                                Min = v.TryGetProperty("min", out var mn) ? mn.GetDouble() : 0,
                                Max = v.TryGetProperty("max", out var mx) ? mx.GetDouble() : 100,
                                DefaultVal = v.TryGetProperty("defaultVal", out var dv) ? dv.GetDouble() : 1,
                                Step = v.TryGetProperty("step", out var st) ? st.GetDouble() : 1
                            });
                    if (!string.IsNullOrEmpty(entry.Formula))
                        result.Formulas.Add(entry);
                }
                return result;
            }
            catch { return new FormulaVarsResponse(); }
        }

        public async Task<CrossConnectionResponse> GetCrossConnectionsAsync(int postId, List<string> allPostTitles)
        {
            var post = await _postService.GetById(postId);
            var content = PrepareArticleContext(post?.Versions?.OrderByDescending(v => v.GradeLevel ?? int.MinValue).FirstOrDefault()?.Content ?? string.Empty);
            var title = post?.Title ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return new CrossConnectionResponse();

            var titlesSnippet = string.Join(", ", allPostTitles.Take(80));
            var systemPrompt = "Jsi interdisciplinární pedagog. Najdi max. 3 konceptuální propojení mezi tímto článkem a tématy z jiných předmětů. Vrať výhradně jako JSON array: [{\"targetPostTitle\": \"přesný název tématu ze seznamu\", \"subject\": \"Předmět (Fyzika/Chemie/...)\", \"explanation\": \"Stručné vysvětlení propojení (1 věta)\"}].";
            var userPrompt = $"Článek: {title}\n\nDostupná témata: {titlesSnippet}\n\nObsah:\n{content}";
            var jsonRaw = await AskRawJsonAsync(systemPrompt, userPrompt);

            try
            {
                using var doc = JsonDocument.Parse(jsonRaw);
                var root = doc.RootElement;
                var connections = new List<CrossConnection>();
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var el in root.EnumerateArray())
                        connections.Add(new CrossConnection
                        {
                            TargetPostTitle = el.TryGetProperty("targetPostTitle", out var tp) ? tp.GetString() ?? "" : "",
                            Subject = el.TryGetProperty("subject", out var sp) ? sp.GetString() ?? "" : "",
                            Explanation = el.TryGetProperty("explanation", out var ep) ? ep.GetString() ?? "" : ""
                        });
                return new CrossConnectionResponse { Connections = connections };
            }
            catch { return new CrossConnectionResponse(); }
        }
    }
}
