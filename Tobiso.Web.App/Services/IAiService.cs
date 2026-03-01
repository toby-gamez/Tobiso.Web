using System.Threading.Tasks;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Services
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
    }
}
