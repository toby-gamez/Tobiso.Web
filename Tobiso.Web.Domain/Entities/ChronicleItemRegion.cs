namespace Tobiso.Web.Domain.Entities;

public class ChronicleItemRegion
{
    public int ItemId { get; set; }
    public ChronicleItem? Item { get; set; }
    public int RegionId { get; set; }
    public ChronicleRegion? Region { get; set; }
}
