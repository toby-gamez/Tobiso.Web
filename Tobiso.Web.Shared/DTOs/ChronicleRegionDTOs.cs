using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class ChronicleRegionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChronicleRegionType Type { get; set; }
    public int? ParentId { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class CreateChronicleRegionRequest
{
    public string Name { get; set; } = string.Empty;
    public ChronicleRegionType Type { get; set; }
    public int? ParentId { get; set; }
}
