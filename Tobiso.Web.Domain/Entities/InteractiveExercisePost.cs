namespace Tobiso.Web.Domain.Entities;

public class InteractiveExercisePost
{
    public int InteractiveExerciseId { get; set; }
    public InteractiveExercise InteractiveExercise { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}
