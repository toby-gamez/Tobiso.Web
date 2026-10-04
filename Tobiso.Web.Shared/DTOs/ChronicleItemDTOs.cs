using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class ChronicleItemResponse
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;

    /// <summary>"Event" or "Person" — see <see cref="ChronicleItemTypeConstants"/>.</summary>
    public string ItemType { get; set; } = ChronicleItemTypeConstants.Event;

    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }

    public long StartYear { get; set; }
    public long? EndYear { get; set; }

    public ChronicleDatePrecision Precision { get; set; }
    public ChronicleReliabilityLevel Reliability { get; set; }
    public string? ReliabilityNote { get; set; }
    public byte Importance { get; set; } = 3;

    public int? MinGradeId { get; set; }
    public int? MinGradeLevel { get; set; }
    public string? LessonSlug { get; set; }

    /// <summary>Person only.</summary>
    public string? Occupation { get; set; }

    public List<ChronicleItemCategoryRef> Categories { get; set; } = new();
    public List<ChronicleRegionRef> Regions { get; set; } = new();
    public List<ChronicleSourceResponse> Sources { get; set; } = new();

    /// <summary>Convenience field: the primary category's color, resolved server-side for rendering.</summary>
    public string? PrimaryColor { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ChronicleItemCategoryRef
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsPrimary { get; set; }
}

public class ChronicleRegionRef
{
    public int RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ChronicleSourceResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Note { get; set; }
}

public class CreateChronicleItemRequest
{
    public string ItemType { get; set; } = ChronicleItemTypeConstants.Event;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public long StartYear { get; set; }
    public long? EndYear { get; set; }
    public ChronicleDatePrecision Precision { get; set; } = ChronicleDatePrecision.Year;
    public ChronicleReliabilityLevel Reliability { get; set; } = ChronicleReliabilityLevel.Certain;
    public string? ReliabilityNote { get; set; }
    public byte Importance { get; set; } = 3;
    public int? MinGradeId { get; set; }
    public string? LessonSlug { get; set; }
    public string? Occupation { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public int? PrimaryCategoryId { get; set; }
    public List<int> RegionIds { get; set; } = new();
}

public class UpdateChronicleItemRequest : CreateChronicleItemRequest
{
}

public class CreateChronicleSourceRequest
{
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Note { get; set; }
}
