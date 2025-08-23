using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoAnonymApi
{
    [Get("/api/Pages")]
    Task<IList<PostResponse>> GetAllPosts();

    [Get("/api/Pages/{id}")]
    Task<PostResponse> GetPostById(int id);
}
