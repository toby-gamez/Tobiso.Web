namespace Tobiso.Web.Domain.Entities;

/// <summary>Join table linking an AI chat session to the extra posts a student attached to the
/// conversation via the paperclip picker. Persisted so attachments survive page reloads and
/// session navigation; the session's own PostId remains the primary anchored article.</summary>
public class AiChatSessionPost
{
    public int AiChatSessionId { get; set; }
    public AiChatSession AiChatSession { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}