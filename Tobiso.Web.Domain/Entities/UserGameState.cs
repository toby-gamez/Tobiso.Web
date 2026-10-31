namespace Tobiso.Web.Domain.Entities;

// One opaque, app-defined JSON document per user (shop, streak, pet, local points...). Revision gives
// optimistic concurrency. The server never trusts anything in it; real-value exchanges use PointsLedgerEntry.
public class UserGameState
{
    public int UserId { get; set; }
    public int Revision { get; set; }
    public string Json { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
