namespace Tobiso.Web.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public int Credits { get; set; } = 20;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<AiChatSession> ChatSessions { get; set; } = [];
    public ICollection<AiCreditTransaction> CreditTransactions { get; set; } = [];
    public ICollection<UserBookmark> Bookmarks { get; set; } = [];
    public ICollection<UserReadPost> ReadPosts { get; set; } = [];
}
