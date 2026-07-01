namespace Tobiso.Web.Domain.Entities;

public class PostDifficultyRating
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int Rating { get; set; }             // 1=easy, 2=ok, 3=hard
    public string DeviceId { get; set; } = "";  // prevents duplicate votes per device
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
