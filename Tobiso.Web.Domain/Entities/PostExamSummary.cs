namespace Tobiso.Web.Domain.Entities;

public class PostExamSummary
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public string Summary { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
