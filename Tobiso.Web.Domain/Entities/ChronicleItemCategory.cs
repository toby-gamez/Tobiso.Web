namespace Tobiso.Web.Domain.Entities;

public class ChronicleItemCategory
{
    public int ItemId { get; set; }
    public ChronicleItem? Item { get; set; }
    public int CategoryId { get; set; }
    public ChronicleCategory? Category { get; set; }
    public bool IsPrimary { get; set; }
}
