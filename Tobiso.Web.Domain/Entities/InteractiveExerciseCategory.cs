namespace Tobiso.Web.Domain.Entities;

public class InteractiveExerciseCategory
{
    public int InteractiveExerciseId { get; set; }
    public InteractiveExercise InteractiveExercise { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
