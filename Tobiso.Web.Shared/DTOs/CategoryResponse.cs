namespace Tobiso.Web.Shared.DTOs;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}

