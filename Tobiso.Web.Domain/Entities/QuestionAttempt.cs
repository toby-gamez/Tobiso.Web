namespace Tobiso.Web.Domain.Entities;

public class QuestionAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QuestionId { get; set; }
    public bool LastCorrect { get; set; }
    public int TimesCorrect { get; set; }
    public int TimesWrong { get; set; }
    public DateTime FirstAttemptedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAttemptedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
