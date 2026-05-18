namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Request DTO for updating post metadata only (title, filepath, category).
/// Version content is managed separately via PostVersions endpoints.
/// </summary>
public class UpdatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
}
