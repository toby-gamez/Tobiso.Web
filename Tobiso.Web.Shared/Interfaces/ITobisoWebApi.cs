using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoWebApi
{
    // Posts (admin CRUD)
    [Get("/api/Posts")]
    Task<IList<PostResponse>> GetAllPosts([Query] int? gradeId = null);

    [Get("/api/Posts/summaries")]
    Task<IList<PostSummaryResponse>> GetPostSummaries();

    [Get("/api/Posts/links")]
    Task<IList<PostLinkResponse>> GetPostLinks();

    [Get("/api/Posts/{id}")]
    Task<PostResponse> GetPostById(int id, [Query] int? gradeId = null);

    [Post("/api/Posts")]
    Task<PostResponse> CreatePost([Body] CreatePostRequest req);

    [Put("/api/Posts/{id}")]
    Task UpdatePost(int id, [Body] UpdatePostRequest req);

    [Delete("/api/Posts/{id}")]
    Task DeletePost(int id);

    // PostVersions
    [Get("/api/PostVersions/by-post/{postId}")]
    Task<IList<PostVersionResponse>> GetVersionsByPost(int postId);

    [Post("/api/PostVersions")]
    Task<PostVersionResponse> CreateVersion([Body] CreateVersionRequest req);

    [Put("/api/PostVersions/{id}")]
    Task UpdateVersion(int id, [Body] UpdateVersionRequest req);

    [Patch("/api/PostVersions/{id}/grade")]
    Task PatchVersionGrade(int id, [Body] UpdateVersionGradeRequest req);

    [Delete("/api/PostVersions/{id}")]
    Task DeleteVersion(int id);

    // Grades
    [Get("/api/Grades")]
    Task<IList<GradeResponse>> GetGrades();

    [Get("/api/Grades/{id}")]
    Task<GradeResponse> GetGradeById(int id);

    [Post("/api/Grades")]
    Task<GradeResponse> CreateGrade([Body] CreateGradeRequest req);

    [Put("/api/Grades/{id}")]
    Task UpdateGrade(int id, [Body] UpdateGradeRequest req);

    [Delete("/api/Grades/{id}")]
    Task DeleteGrade(int id);

    [Post("/api/Grades/seed")]
    Task SeedGrades();

    // Categories
    [Get("/api/Categories")]
    Task<IList<CategoryResponse>> GetAllCategories();

    [Get("/api/Categories/tree")]
    Task<IList<CategoryTreeResponse>> GetCategoryTree();

    [Get("/api/Categories/ancestors/{categoryId}")]
    Task<IList<CategoryResponse>> GetCategoryAncestors(int categoryId);

    [Post("/api/Categories")]
    Task<CategoryResponse> CreateCategory([Body] CategoryResponse category);

    [Put("/api/Categories/{id}")]
    Task<CategoryResponse> UpdateCategory(int id, [Body] CategoryResponse category);

    [Delete("/api/Categories/{id}")]
    Task DeleteCategory(int id);

    // Events
    [Get("/api/Events")]
    Task<IList<EventResponse>> GetAllEvents();

    [Get("/api/Events/{id}")]
    Task<EventResponse> GetEventById(int id);

    [Get("/api/Events?startDate={startDate}&endDate={endDate}")]
    Task<IList<EventResponse>> GetEventsByDateRange(DateTime startDate, DateTime endDate);

    [Get("/api/Events/search?searchTerm={searchTerm}")]
    Task<IList<EventResponse>> SearchEvents(string searchTerm);

    [Post("/api/Events")]
    Task<EventResponse> CreateEvent([Body] CreateEventRequest request);

    [Put("/api/Events/{id}")]
    Task UpdateEvent(int id, [Body] UpdateEventRequest request);

    [Delete("/api/Events/{id}")]
    Task DeleteEvent(int id);

    // RelatedPosts
    [Get("/api/RelatedPosts")]
    Task<IList<RelatedPostResponse>> GetAllRelatedPosts();

    [Get("/api/RelatedPosts/by-post/{postId}")]
    Task<IList<RelatedPostResponse>> GetRelatedPostsByPostId(int postId);

    [Get("/api/RelatedPosts/{id}")]
    Task<RelatedPostResponse> GetRelatedPostById(int id);

    [Post("/api/RelatedPosts")]
    Task<RelatedPostResponse> CreateRelatedPost([Body] CreateRelatedPostRequest request);

    [Put("/api/RelatedPosts/{id}")]
    Task UpdateRelatedPost(int id, [Body] UpdateRelatedPostRequest request);

    [Delete("/api/RelatedPosts/{id}")]
    Task DeleteRelatedPost(int id);

    // Addendums
    [Get("/api/Addendums")]
    Task<IList<AddendumResponse>> GetAllAddendums();

    [Get("/api/Addendums/{id}")]
    Task<AddendumResponse> GetAddendumById(int id);

    [Post("/api/Addendums")]
    Task<AddendumResponse> CreateAddendum([Body] AddendumResponse addendum);

    [Put("/api/Addendums/{id}")]
    Task UpdateAddendum(int id, [Body] AddendumResponse addendum);

    [Delete("/api/Addendums/{id}")]
    Task DeleteAddendum(int id);

    // Files
    [Get("/api/files")]
    Task<IList<FileUploadResponse>> GetAllFilesAsync([Query] string? subDirectory = null);

    [Multipart]
    [Post("/api/files/upload")]
    Task<FileUploadResponse> UploadImageAsync([AliasAs("file")] StreamPart stream);

    [Delete("/api/files/{fileName}")]
    Task DeleteImageAsync(string fileName);

    // Grammar check
    [Post("/api/ai/grammar-check")]
    Task<GrammarCheckResponse> CheckGrammar([Body] GrammarCheckRequest request);
}
