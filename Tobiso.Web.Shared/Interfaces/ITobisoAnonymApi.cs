using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoAnonymApi
{
    [Get("/api/Pages")]
    Task<IList<PostResponse>> GetAllPosts();

    [Get("/api/Pages/{id}")]
    Task<PostResponse> GetPostById(int id);

    [Get("/api/Categories")]
    Task<List<CategoryResponse>> GetAllCategories();

    [Get("/api/Categories/tree")]
    Task<CategoryTreeResponse> GetTree();

    [Get("/api/Questions")]
    Task<List<QuestionResponse>> GetAllQuestions();

    [Get("/api/Questions/post/{postId}")]
    Task<List<QuestionResponse>> GetQuestionsByPostId(int postId);

    // Events API - anonymní přístup pro čtení
    [Get("/api/Events")]
    Task<List<EventResponse>> GetAllEvents();

    [Get("/api/Events/range")]
    Task<List<EventResponse>> GetEventsByDateRange([Query] DateTime startDate, [Query] DateTime endDate);

    [Get("/api/Events/{id}")]
    Task<EventResponse> GetEventById(int id);
}
