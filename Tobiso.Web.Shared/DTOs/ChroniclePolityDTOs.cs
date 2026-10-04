using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class ChroniclePolityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long StartYear { get; set; }
    public long EndYear { get; set; }
}

public class ChroniclePolityTerritoryResponse
{
    public int Id { get; set; }
    public int PolityId { get; set; }
    public string PolityName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public ChronicleCoverage Coverage { get; set; }
    public long? FromYear { get; set; }
    public long? ToYear { get; set; }
}

/// <summary>"V té době: Velká Morava (část dnešního území)" style label for a region+year.</summary>
public class ChroniclePolityLabel
{
    public string PolityName { get; set; } = string.Empty;
    public ChronicleCoverage Coverage { get; set; }
}

public class CreateChroniclePolityRequest
{
    public string Name { get; set; } = string.Empty;
    public long StartYear { get; set; }
    public long EndYear { get; set; }
}

public class CreateChroniclePolityTerritoryRequest
{
    public int PolityId { get; set; }
    public int RegionId { get; set; }
    public ChronicleCoverage Coverage { get; set; }
    public long? FromYear { get; set; }
    public long? ToYear { get; set; }
}
