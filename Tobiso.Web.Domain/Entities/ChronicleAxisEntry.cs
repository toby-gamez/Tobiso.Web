namespace Tobiso.Web.Domain.Entities;

/// <summary>A link between an axis and either one item or one whole category (not a copy of the item itself).</summary>
public class ChronicleAxisEntry
{
    public int Id { get; set; }
    public int AxisId { get; set; }
    public ChronicleAxis? Axis { get; set; }

    public int? ItemId { get; set; }
    public ChronicleItem? Item { get; set; }

    public int? CategoryId { get; set; }
    public ChronicleCategory? Category { get; set; }

    public bool ExpandChildren { get; set; }
    public int? OrderOverride { get; set; }
}
