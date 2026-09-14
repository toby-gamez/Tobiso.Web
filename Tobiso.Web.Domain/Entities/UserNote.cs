namespace Tobiso.Web.Domain.Entities;

public class UserNote
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
