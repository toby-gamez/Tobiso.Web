using System.Collections.Generic;

namespace Tobiso.Web.Shared.DTOs
{
    public class AiChatRequest
    {
        public int PostId { get; set; }
        public string Question { get; set; } = string.Empty;
        public List<AiMessage>? ConversationHistory { get; set; }
        public bool SocraticMode { get; set; } = false;
        /// <summary>IDs of additional posts the user attached to this question, whose content is added as extra context.</summary>
        public List<int> AttachedPostIds { get; set; } = new();
    }
}
