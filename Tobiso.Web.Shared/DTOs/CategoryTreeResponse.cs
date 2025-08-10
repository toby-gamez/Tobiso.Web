namespace Tobiso.Web.Shared.DTOs;

public class CategoryTreeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CategoryTreeResponse> Children { get; set; } = new();
}
