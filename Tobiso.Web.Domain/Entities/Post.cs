namespace Tobiso.Web.Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // Renamed: UpdatedAt -> LastFix
    public DateTime? LastFix { get; set; }
    // New: last edit timestamp (content edit)
    public DateTime? LastEdit { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<InteractiveExercisePost> InteractiveExercisePosts { get; set; } = new List<InteractiveExercisePost>();
}
