namespace Tobiso.Web.Shared.DTOs;

public class PostSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime? LastFix { get; set; }
    public DateTime? LastEdit { get; set; }
}
