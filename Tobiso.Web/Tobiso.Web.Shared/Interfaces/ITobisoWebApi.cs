using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoWebApi
{
    [Get("/api/Posts")]
    Task<IList<PostResponse>> GetAllPosts();

    [Get("/api/Categories/tree")]
    Task<IList<CategoryTreeResponse>> GetCategoryTree();
}
