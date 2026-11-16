namespace Tobiso.Web.Shared.DTOs;

/// <summary>Sets the manual order of posts in a category. PostIds are listed in the desired order.</summary>
public class ReorderPostsRequest
{
    public int CategoryId { get; set; }
    public List<int> PostIds { get; set; } = new();
}
