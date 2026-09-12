using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Helpers;

/// <summary>
/// Lucide icon name and Czech display label for each <see cref="ExerciseTypeConstants"/> value.
/// Shared between article pages and the practice hub so both stay in sync.
/// </summary>
public static class ExerciseTypeDisplay
{
    public static string Icon(string type) => type switch
    {
        ExerciseTypeConstants.DragDrop => "move",
        ExerciseTypeConstants.Matching => "link-2",
        ExerciseTypeConstants.Timeline => "history",
        ExerciseTypeConstants.Circuit => "zap",
        ExerciseTypeConstants.Molecule => "atom",
        _ => "puzzle"
    };

    public static string Label(string type) => type switch
    {
        ExerciseTypeConstants.DragDrop => "Přetahování",
        ExerciseTypeConstants.Matching => "Párování",
        ExerciseTypeConstants.Timeline => "Časová osa",
        ExerciseTypeConstants.Circuit => "Obvod",
        ExerciseTypeConstants.Molecule => "Molekula",
        _ => "Cvičení"
    };
}
