namespace Tobiso.Web.Domain.Entities;

public class ChroniclePolity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long StartYear { get; set; }
    public long EndYear { get; set; }

    public ICollection<ChroniclePolityTerritory> Territories { get; set; } = new List<ChroniclePolityTerritory>();
}
