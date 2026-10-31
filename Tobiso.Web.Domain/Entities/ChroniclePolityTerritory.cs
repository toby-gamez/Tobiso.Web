namespace Tobiso.Web.Domain.Entities;

public class ChroniclePolityTerritory
{
    public int Id { get; set; }
    public int PolityId { get; set; }
    public ChroniclePolity? Polity { get; set; }
    public int RegionId { get; set; }
    public ChronicleRegion? Region { get; set; }
    public ChronicleCoverage Coverage { get; set; }

    /// <summary>Null = po celou dobu existence polity.</summary>
    public long? FromYear { get; set; }
    public long? ToYear { get; set; }
}
