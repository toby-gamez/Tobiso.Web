namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Request DTO for updating an existing post version's content.
/// GradeId cannot be changed after creation (use delete + create instead).
/// </summary>
public class UpdateVersionRequest
{
    public string Content { get; set; } = string.Empty;
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
}
