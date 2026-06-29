namespace Tobiso.Web.Domain.Entities;

public class UserReadPost
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public int ScrollPercent { get; set; }
    public DateTime FirstReadAt { get; set; } = DateTime.UtcNow;
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
