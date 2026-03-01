using System;

namespace Tobiso.Web.Shared.DTOs
{
    public class AiChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public int RemainingQuestions { get; set; }
    }
}
