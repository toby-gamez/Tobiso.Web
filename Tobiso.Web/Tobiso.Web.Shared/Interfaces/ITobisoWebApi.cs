using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoWebApi
{
    [Get("/api/Posts")]
    Task<IList<PostResponse>> GetAllPosts();

    [Get("/api/Categories/tree")]
    Task<IList<CategoryTreeResponse>> GetCategoryTree();

    [Get("/api/Posts?categoryId={categoryId}")]
    Task<IList<PostResponse>> GetPostsByCategory(int categoryId);

    [Get("/api/Posts/{id}")]
    Task<PostResponse> GetPostById(int id);

    [Put("/api/Posts/{id}")]
    Task UpdatePost(int id, [Body] PostResponse post);

    [Get("/api/Categories")]
    Task<IList<CategoryResponse>> GetAllCategories();

    [Delete("/api/Posts/{id}")]
    Task DeletePost(int id);

    [Post("/api/Posts")]
    Task<PostResponse> CreatePost([Body] PostResponse post);
}
