namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// A post whose content (not just title) matched a full-text search query, with the
/// matched phrase and its surrounding word for a snippet preview.
/// </summary>
public class PostSearchResultDto
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? GradeId { get; set; }
    public string? GradeName { get; set; }

    /// <summary>The word immediately preceding the match, if any.</summary>
    public string? WordBefore { get; set; }
    /// <summary>The matched phrase (whole word(s) containing the query), in its original casing.</summary>
    public string MatchWord { get; set; } = string.Empty;
    /// <summary>The word immediately following the match, if any.</summary>
    public string? WordAfter { get; set; }
}
