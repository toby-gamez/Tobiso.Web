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
    }
}
