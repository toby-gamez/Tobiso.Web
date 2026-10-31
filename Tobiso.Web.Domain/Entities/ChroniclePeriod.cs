namespace Tobiso.Web.Domain.Entities;

public class ChroniclePeriod
{
    public int Id { get; set; }
    public int PeriodizationId { get; set; }
    public ChroniclePeriodization? Periodization { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Used when <see cref="StartEventId"/> is null.</summary>
    public long StartYear { get; set; }

    /// <summary>When set, the period's effective start is that event's StartYear (e.g. "novověk začíná pádem Konstantinopole").</summary>
    public int? StartEventId { get; set; }
    public ChronicleItem? StartEvent { get; set; }

    public string? Color { get; set; }

    /// <summary>Default collapsed/expanded state for this era on the axis — distant/sparse eras
    /// (pravěk) default collapsed, recent ones default expanded. Per-period, not global.</summary>
    public bool DefaultCollapsed { get; set; } = true;
}
