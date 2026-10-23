namespace Tobiso.Web.Domain.Entities;

// One opaque, app-defined JSON document per user (points, shop, streak, pet...). Revision gives
// optimistic concurrency. Points mirrors state.points.value so the server can gate real-value
// exchanges (points -> AI credits) without parsing the blob.
public class UserGameState
{
    public int UserId { get; set; }
    public int Revision { get; set; }
    public string Json { get; set; } = "{}";
    public double Points { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
