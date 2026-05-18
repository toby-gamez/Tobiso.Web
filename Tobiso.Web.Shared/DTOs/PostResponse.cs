namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Response DTO for a post.
/// Content and timestamps live exclusively in <see cref="Versions"/>.
/// When gradeId is passed to the API, Versions contains only the single best-matching version.
/// When no gradeId is passed, Versions contains all versions (useful for admin grade switching).
/// </summary>
public class PostResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public List<PostVersionResponse> Versions { get; set; } = new();
}
