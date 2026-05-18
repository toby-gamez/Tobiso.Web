namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Request DTO for creating a new version of a post for a specific grade.
/// </summary>
public class CreateVersionRequest
{
    public int PostId { get; set; }
    public int GradeId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
}
