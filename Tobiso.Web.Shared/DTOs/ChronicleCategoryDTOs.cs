namespace Tobiso.Web.Shared.DTOs;

public class ChronicleCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Color { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public int ItemCount { get; set; }
}

public class ChronicleCategoryTreeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public List<ChronicleCategoryTreeResponse> Children { get; set; } = new();
}

public class CreateChronicleCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Color { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
}
