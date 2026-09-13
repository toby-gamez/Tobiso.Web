namespace Tobiso.Web.Domain.Entities;

public class AnonymousAiUsage
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = "";
    public int Count { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
