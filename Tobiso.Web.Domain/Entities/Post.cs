namespace Tobiso.Web.Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // Content moved to PostVersion to allow multiple versions (per grade)
    public ICollection<PostVersion> Versions { get; set; } = new List<PostVersion>();
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<InteractiveExercisePost> InteractiveExercisePosts { get; set; } = new List<InteractiveExercisePost>();
}
