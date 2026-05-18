namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Request DTO for creating a new post with its initial version.
/// </summary>
public class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? CategoryId { get; set; }

    // Initial version data — when GradeId is null no version is created (e.g. bulk import)
    public int? GradeId { get; set; }
    public string Content { get; set; } = string.Empty;
    /// <summary>True = minor fix (LastFix), false = major edit (LastEdit).</summary>
    public bool IsFix { get; set; } = false;
}
