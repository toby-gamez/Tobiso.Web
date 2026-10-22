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
    // Null until the address is proven (verification link, password reset link or Google sign-in).
    public DateTime? EmailConfirmedAt { get; set; }
    // Copied into every token; changing it invalidates all of the user's existing sessions.
    public string SecurityStamp { get; set; } = NewSecurityStamp();
    public DateTime? TermsAcceptedAt { get; set; }
    public string? TermsVersion { get; set; }
    // Change date (LastEdit of the legal articles) of the last notice the user closed; suppresses re-showing it.
    public DateTime? LegalNoticeDismissedFor { get; set; }
    public int? PreferredGradeId { get; set; }
    public DateTime? LastDailyBonusAt { get; set; }
    public DateTime? LastReadBonusAt { get; set; }

    public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");

    public ICollection<AiChatSession> ChatSessions { get; set; } = [];
    public ICollection<AiCreditTransaction> CreditTransactions { get; set; } = [];
    public ICollection<UserBookmark> Bookmarks { get; set; } = [];
    public ICollection<UserNote> Notes { get; set; } = [];
    public ICollection<UserReadPost> ReadPosts { get; set; } = [];
    public ICollection<QuestionAttempt> QuestionAttempts { get; set; } = [];
}
