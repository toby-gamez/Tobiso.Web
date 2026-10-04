namespace Tobiso.Web.Shared.DTOs;

public class ChronicleAxisResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class ChronicleAxisEntryResponse
{
    public int Id { get; set; }
    public int AxisId { get; set; }
    public int? ItemId { get; set; }
    public string? ItemTitle { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool ExpandChildren { get; set; }
    public int? OrderOverride { get; set; }
}

public class CreateChronicleAxisRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class CreateChronicleAxisEntryRequest
{
    public int AxisId { get; set; }
    public int? ItemId { get; set; }
    public int? CategoryId { get; set; }
    public bool ExpandChildren { get; set; }
    public int? OrderOverride { get; set; }
}
