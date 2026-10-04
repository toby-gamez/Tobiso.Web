namespace Tobiso.Web.Domain.Entities;

public class ChronicleSource
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public ChronicleItem? Item { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Note { get; set; }
}
