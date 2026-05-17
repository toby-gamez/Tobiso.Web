using Tobiso.Web.Shared.DTOs;
using System.Threading.Tasks;

namespace Tobiso.Web.Shared.Interfaces
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
        // Detects person names in the provided raw text content. Returns a list of names.
        Task<List<string>> DetectPeopleInTextAsync(string content);
    }
}
