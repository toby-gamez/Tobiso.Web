namespace Tobiso.Web.Domain.Entities;

public class ChronicleRegion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChronicleRegionType Type { get; set; }
    public int? ParentId { get; set; }
    public ChronicleRegion? Parent { get; set; }

    /// <summary>Materialized path, e.g. "/1/2/15/", so descendants resolve via a single prefix LIKE query.</summary>
    public string Path { get; set; } = string.Empty;

    public ICollection<ChronicleItemRegion> Items { get; set; } = new List<ChronicleItemRegion>();
    public ICollection<ChroniclePolityTerritory> PolityTerritories { get; set; } = new List<ChroniclePolityTerritory>();
}
