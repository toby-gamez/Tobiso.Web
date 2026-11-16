namespace Tobiso.Web.Shared.DTOs;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    /// <summary>Label of the manual post order (e.g. "Historicky"); null/empty = posts are not manually sorted.</summary>
    public string? SortLabel { get; set; }
}
