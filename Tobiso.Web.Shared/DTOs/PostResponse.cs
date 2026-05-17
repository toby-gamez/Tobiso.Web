using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class PostResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    // Replaced UpdatedAt with LastFix and added LastEdit
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
    public int? CategoryId { get; set; }
    // Provided when returning a matched version
    public int? GradeId { get; set; }
}
