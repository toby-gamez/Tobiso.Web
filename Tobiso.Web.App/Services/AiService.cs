using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Services
{
    public class AiService : IAiService
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
    }
}
