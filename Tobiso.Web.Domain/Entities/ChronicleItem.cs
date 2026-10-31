namespace Tobiso.Web.Domain.Entities;

/// <summary>
/// Shared base for kronika items (events and people), mapped via EF Core TPH
/// so causality links, categories, regions and sources can all hang off one Id/FK shape.
/// </summary>
public abstract class ChronicleItem
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }

    /// <summary>Negative = př. n. l. For a person, Start/End = birth/death.</summary>
    public long StartYear { get; set; }
    public long? EndYear { get; set; }

    public ChronicleDatePrecision Precision { get; set; }
    public ChronicleReliabilityLevel Reliability { get; set; }
    public string? ReliabilityNote { get; set; }
    public byte Importance { get; set; } = 3;

    public int? MinGradeId { get; set; }
    public Grade? MinGrade { get; set; }

    public string? LessonSlug { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ChronicleItemCategory> Categories { get; set; } = new List<ChronicleItemCategory>();
    public ICollection<ChronicleItemRegion> Regions { get; set; } = new List<ChronicleItemRegion>();
    public ICollection<ChronicleSource> Sources { get; set; } = new List<ChronicleSource>();
}
