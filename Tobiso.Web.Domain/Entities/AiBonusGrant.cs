namespace Tobiso.Web.Domain.Entities;

/// <summary>
/// Bonus AI requests bought by a device (a signed grant redeemed via /api/ai/credits). Persisted so a
/// purchase survives app restarts, and so a signed grant can be redeemed only once: the unique
/// <see cref="SignatureHash"/> is the replay protection.
/// </summary>
public class AiBonusGrant
{
    public int Id { get; set; }
    /// <summary>The device key ("device:{id}") the grant was redeemed for.</summary>
    public string DeviceKey { get; set; } = "";
    public int Count { get; set; }
    public DateTime ValidUntil { get; set; }
    /// <summary>SHA-256 (hex) of the grant's signature.</summary>
    public string SignatureHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
