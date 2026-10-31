namespace Tobiso.Web.Domain.Entities;

public class ChronicleAxis
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<ChronicleAxisEntry> Entries { get; set; } = new List<ChronicleAxisEntry>();
}
