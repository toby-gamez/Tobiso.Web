using Refit;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

public interface ITobisoWebApi
{
    // todo: odstranit, až bude náhrada, je anonymní
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

    [Post("/api/Categories")]
    Task<CategoryResponse> CreateCategory([Body] CategoryResponse category);

    [Put("/api/Categories/{id}")]
    Task<CategoryResponse> UpdateCategory(int id, [Body] CategoryResponse category);

    [Delete("/api/Categories/{id}")]
    Task DeleteCategory(int id);

    [Get("/api/Posts/links")]
    Task<IList<PostLinkResponse>> GetPostLinks();

    // Events API
    [Get("/api/Events")]
    Task<IList<EventResponse>> GetAllEvents();

    [Get("/api/Events?startDate={startDate}&endDate={endDate}")]
    Task<IList<EventResponse>> GetEventsByDateRange(DateTime startDate, DateTime endDate);

    [Get("/api/Events/{id}")]
    Task<EventResponse> GetEventById(int id);

    [Post("/api/Events")]
    Task<EventResponse> CreateEvent([Body] CreateEventRequest request);

    [Put("/api/Events/{id}")]
    Task UpdateEvent(int id, [Body] UpdateEventRequest request);

    [Delete("/api/Events/{id}")]
    Task DeleteEvent(int id);

    [Get("/api/Events/search?searchTerm={searchTerm}")]
    Task<IList<EventResponse>> SearchEvents(string searchTerm);

    // RelatedPosts API
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

    // Addendums API
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

    // File upload endpoints - STEJNĚ jako SentrySMP
    [Get("/api/files")]
    Task<IList<FileUploadResponse>> GetAllFilesAsync([Query] string? subDirectory = null);
    
    [Multipart]
    [Post("/api/files/upload")]
    Task<FileUploadResponse> UploadImageAsync([AliasAs("file")] StreamPart stream);
    
    [Delete("/api/files/{fileName}")]
    Task DeleteImageAsync(string fileName);
}
