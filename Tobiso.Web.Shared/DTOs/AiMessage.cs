namespace Tobiso.Web.Shared.DTOs
{
    public class AiMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant" or "system"
        public string Content { get; set; } = string.Empty;
    }
}
