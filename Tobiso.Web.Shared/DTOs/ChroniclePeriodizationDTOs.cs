namespace Tobiso.Web.Shared.DTOs;

public class ChroniclePeriodizationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<ChroniclePeriodResponse> Periods { get; set; } = new();
}

public class ChroniclePeriodResponse
{
    public int Id { get; set; }
    public int PeriodizationId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Resolved effective start: StartEvent's StartYear when set, else StartYear.</summary>
    public long EffectiveStartYear { get; set; }
    public int? StartEventId { get; set; }
    public string? Color { get; set; }
    public bool DefaultCollapsed { get; set; }
}

public class CreateChroniclePeriodizationRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class CreateChroniclePeriodRequest
{
    public int PeriodizationId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public long StartYear { get; set; }
    public int? StartEventId { get; set; }
    public string? Color { get; set; }
    public bool DefaultCollapsed { get; set; } = true;
}
