namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Known exercise type identifiers stored in <see cref="InteractiveExercise.Type"/>.
/// Use these constants instead of magic strings wherever the type is referenced.
/// </summary>
public static class ExerciseTypeConstants
{
    public const string Circuit  = "circuit";
    public const string Timeline = "timeline";
    public const string DragDrop = "drag-drop";
    public const string Molecule = "molecule";
    public const string Matching = "matching";

    /// <summary>All recognised type values.</summary>
    public static readonly IReadOnlyList<string> All =
        new[] { Circuit, Timeline, DragDrop, Molecule, Matching };

    /// <summary>Returns true when <paramref name="type"/> is a recognised exercise type.</summary>
    public static bool IsKnown(string? type) =>
        type != null && All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
