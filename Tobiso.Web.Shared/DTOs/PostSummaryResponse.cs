namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Lightweight summary of a post — metadata plus the most recent edit/fix dates
/// (aggregated across all versions) for display in list views.
/// </summary>
public class PostSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    /// <summary>Most recent LastEdit timestamp across all versions, for "last updated" display.</summary>
    public DateTime? LastEdit { get; set; }
    /// <summary>Most recent LastFix timestamp across all versions, for "last updated" display.</summary>
    public DateTime? LastFix { get; set; }
    /// <summary>Grade names available for this post, ordered by level (e.g. ["6. třída", "9. třída"]).</summary>
    public List<string> AvailableGradeNames { get; set; } = new();
}
