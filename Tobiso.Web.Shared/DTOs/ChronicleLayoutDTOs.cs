namespace Tobiso.Web.Shared.DTOs;

/// <summary>Filter/query shape for GET layout requests — also the cache key for ChronicleAxisService.</summary>
public class ChronicleFilter
{
    public List<int>? RegionIds { get; set; }
    public int? PeriodizationId { get; set; }
    public byte? MinImportance { get; set; }
    public int? MaxGradeLevel { get; set; }
    public bool IncludePeople { get; set; }
    public bool PeopleOnly { get; set; }

    /// <summary>Category ids the client has expanded (their events render individually instead of as one collapsed card).</summary>
    public List<int>? ExpandedCategoryIds { get; set; }

    /// <summary>Era (period) names whose DefaultCollapsed state the user has flipped by clicking
    /// (collapsed↔expanded is a toggle off a per-period default, not a one-way reveal).</summary>
    public List<string>? ToggledEraNames { get; set; }

    public string CacheKey()
    {
        var regions = RegionIds is { Count: > 0 } ? string.Join(",", RegionIds.OrderBy(x => x)) : "-";
        var expanded = ExpandedCategoryIds is { Count: > 0 } ? string.Join(",", ExpandedCategoryIds.OrderBy(x => x)) : "-";
        var eras = ToggledEraNames is { Count: > 0 } ? string.Join(",", ToggledEraNames.OrderBy(x => x)) : "-";
        return $"r:{regions}|p:{PeriodizationId}|i:{MinImportance}|g:{MaxGradeLevel}|people:{IncludePeople}|only:{PeopleOnly}|exp:{expanded}|era:{eras}";
    }
}

public enum ChronicleSide { Left, Right }

public class ChronicleCardResponse
{
    public string Key { get; set; } = string.Empty;
    public ChronicleItemResponse Node { get; set; } = null!;
    public ChronicleSide Side { get; set; }
    public int Lane { get; set; }
    public int Top { get; set; }
    public int Height { get; set; }

    /// <summary>True when this card represents a collapsed category rather than a single item.</summary>
    public bool IsCollapsedCategory { get; set; }
    public int? CategoryId { get; set; }
    public int CollapsedCount { get; set; }
}

public class ChronicleMarkerResponse
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnd { get; set; }
    public int Y { get; set; }
    public int Slot { get; set; }
    public string? Color { get; set; }
}

public class ChronicleRowLabelResponse
{
    public long Year { get; set; }
    public int Y { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>Collapsed era: "jeden široký blok přes celou šířku osy s názvem a počtem položek uvnitř
/// („Pravěk · 42 položek“)... zabírá jen jeden řádek."</summary>
public class ChronicleEraBlockResponse
{
    public string Name { get; set; } = string.Empty;
    public int Y { get; set; }
    public int Count { get; set; }
    public string? Color { get; set; }
}

/// <summary>Expanded era: "tenký barevný pruh podél osy (ne karta) přes řádky, které éra pokrývá" —
/// purely an orientation aid; the era's actual items render as normal cards alongside it.</summary>
public class ChronicleEraStripeResponse
{
    public string Name { get; set; } = string.Empty;
    public int YStart { get; set; }
    public int YEnd { get; set; }
    public string? Color { get; set; }
}

public class ChronicleLayoutResponse
{
    public int AxisId { get; set; }
    public string AxisSlug { get; set; } = string.Empty;
    public List<ChronicleCardResponse> Cards { get; set; } = new();
    public List<ChronicleMarkerResponse> Markers { get; set; } = new();
    public List<ChronicleRowLabelResponse> RowLabels { get; set; } = new();
    public List<ChronicleEraBlockResponse> EraBlocks { get; set; } = new();
    public List<ChronicleEraStripeResponse> EraStripes { get; set; } = new();
    public int TotalHeight { get; set; }
}
