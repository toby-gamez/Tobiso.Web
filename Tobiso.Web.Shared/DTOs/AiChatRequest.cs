using System.Collections.Generic;

namespace Tobiso.Web.Shared.DTOs
{
    public class AiChatRequest
    {
        public int PostId { get; set; }
        public string Question { get; set; } = string.Empty;
        public List<AiMessage>? ConversationHistory { get; set; }
    }
}
