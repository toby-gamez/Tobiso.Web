// ...existing code...
[Post("/api/Posts")]
Task<PostResponse> CreatePost([Body] PostResponse post);

[Delete("/api/Posts/{id}")]
Task DeletePost(int id);
// ...existing code...
