namespace Tobiso.Web.Domain.Entities;

public class AiCreditTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
