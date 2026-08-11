namespace Tobiso.Web.Domain.Entities;

public enum FeedbackType { Bug, FeatureRequest }
public enum FeedbackStatus { New, InProgress, Resolved, WontFix }

public class Feedback
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public FeedbackType Type { get; set; } = FeedbackType.Bug;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public string? AdminNote { get; set; }
}
