namespace Tobiso.Web.Domain.Entities;

public class AiChatMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public int? CreditsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiChatSession Session { get; set; } = null!;
}
