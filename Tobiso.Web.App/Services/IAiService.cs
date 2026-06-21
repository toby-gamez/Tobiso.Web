using System.Threading.Tasks;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Services
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
        IAsyncEnumerable<string> AskStreamAsync(AiChatRequest request);
        Task<string> ExplainSentenceAsync(string sentence, string articleContext);
        Task<EvaluateAnswerResponse> EvaluateAnswerAsync(EvaluateAnswerRequest request);
        Task<FlashcardResponse> GenerateFlashcardsAsync(int postId);
    }
}
