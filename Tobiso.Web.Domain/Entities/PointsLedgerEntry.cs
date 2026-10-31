namespace Tobiso.Web.Domain.Entities;

// Server-owned "exchange points": earned only through answers the server graded itself and spent only
// on the points -> AI credits exchange. Unrelated to the cosmetic points the app keeps locally.
// AppUser.PointsBalance is the running sum of Delta; the ledger is the audit trail and the dedupe/cap source.
public class PointsLedgerEntry
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int Delta { get; set; }
    public string Kind { get; set; } = "";
    // Dedupe key within (UserId, Kind), e.g. "<questionId>:<yyyyMMdd>" for quiz answers. Null = never deduped.
    public string? Ref { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
