namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Response DTO for a single post version tied to a specific grade.
/// </summary>
public class PostVersionResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int GradeId { get; set; }
    public string? GradeName { get; set; }
    /// <summary>Convenience: Grade.Level (e.g. 6, 7, 8, 9) for client-side display.</summary>
    public int? GradeLevel { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
}
