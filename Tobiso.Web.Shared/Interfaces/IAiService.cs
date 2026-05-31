using Tobiso.Web.Shared.DTOs;
using System.Threading.Tasks;

namespace Tobiso.Web.Shared.Interfaces
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
        // Detects person names in the provided raw text content. Returns a list of names.
        Task<List<string>> DetectPeopleInTextAsync(string content);
        // Sends a request with a fully custom system prompt and enforces JSON output mode.
        // Returns the raw JSON string from the model. Throws on HTTP or config errors.
        Task<string> AskRawJsonAsync(string systemPrompt, string userPrompt);
        // Check grammar and return identified issues (multilingual)
        Task<GrammarCheckResponse> CheckGrammarAsync(string content);
        // Generate a compact cheat-sheet summary for the given article content.
        // Returns plain text with bullet points (one per line, prefixed with •).
        // ratio: "1x1" → ~20 bullets, "1x2" → ~35 bullets.
        Task<string> GenerateCheatSheetAsync(string title, string content, string ratio = "1x1");
        // Generate one or more questions from article content. Returns pre-filled CreateQuestionRequests
        // (PostId = 0 — caller sets it). For factual questions: 1 answer (correct=1).
        // For conceptual questions: 3–4 answers with one correct.
        // existingQuestions: question texts already saved/queued — AI will avoid duplicating them.
        Task<List<CreateQuestionRequest>> GenerateQuestionsAsync(string content, int count, List<string> existingQuestions);
    }
}
