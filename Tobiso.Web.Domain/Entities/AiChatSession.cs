namespace Tobiso.Web.Domain.Entities;

public class AiChatSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    /// <summary>Null for a general chat session not tied to any article.</summary>
    public int? PostId { get; set; }
    /// <summary>Display name for a general session, derived from its first question. Null for legacy rows predating this column, and unused for per-article sessions (which display the article's title instead).</summary>
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post? Post { get; set; }
    public ICollection<AiChatMessage> Messages { get; set; } = [];
}
