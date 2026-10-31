using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Bulk-import payload (doc §7: hand-entering hundreds of events isn't practical).
/// Items/categories/regions are matched by Slug/Name on re-import so the same file can be re-run idempotently.
/// </summary>
public class ChronicleImportRequest
{
    public List<ChronicleImportCategory> Categories { get; set; } = new();
    public List<ChronicleImportRegion> Regions { get; set; } = new();
    public List<ChronicleImportItem> Items { get; set; } = new();
    public List<ChronicleImportLink> Links { get; set; } = new();

    /// <summary>Slug of the axis to attach every imported item to (created if missing).</summary>
    public string? TargetAxisSlug { get; set; }
    public string? TargetAxisName { get; set; }
}

public class ChronicleImportCategory
{
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public string? Color { get; set; }
    public string? Summary { get; set; }
}

public class ChronicleImportRegion
{
    public string Name { get; set; } = string.Empty;
    public ChronicleRegionType Type { get; set; }
    public string? ParentName { get; set; }
}

public class ChronicleImportItem
{
    public string Slug { get; set; } = string.Empty;
    public string ItemType { get; set; } = ChronicleItemTypeConstants.Event;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public long StartYear { get; set; }
    public long? EndYear { get; set; }
    public ChronicleDatePrecision Precision { get; set; } = ChronicleDatePrecision.Year;
    public ChronicleReliabilityLevel Reliability { get; set; } = ChronicleReliabilityLevel.Certain;
    public string? ReliabilityNote { get; set; }
    public byte Importance { get; set; } = 3;
    public string? Occupation { get; set; }
    public List<string> CategoryNames { get; set; } = new();
    public string? PrimaryCategoryName { get; set; }
    public List<string> RegionNames { get; set; } = new();
}

public class ChronicleImportLink
{
    public string FromSlug { get; set; } = string.Empty;
    public string ToSlug { get; set; } = string.Empty;
    public ChronicleLinkType Type { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class ChronicleImportResult
{
    public int CategoriesCreated { get; set; }
    public int RegionsCreated { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int LinksCreated { get; set; }
    public int AxisEntriesCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}
