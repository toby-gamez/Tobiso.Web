using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class PostResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}
