namespace Tobiso.Web.Domain.Entities;

public class ChronicleCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public ChronicleCategory? Parent { get; set; }
    public List<ChronicleCategory> Children { get; set; } = new();
    public string? Color { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }

    public ICollection<ChronicleItemCategory> Items { get; set; } = new List<ChronicleItemCategory>();
}
