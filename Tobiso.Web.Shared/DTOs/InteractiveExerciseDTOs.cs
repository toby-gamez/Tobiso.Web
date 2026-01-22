namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// Response DTO pro interaktivní cvičení
/// </summary>
public class InteractiveExerciseResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public string? InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO pro vytvoření nového cvičení (Admin)
/// </summary>
public class CreateInteractiveExerciseRequest
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public string SolutionJson { get; set; } = string.Empty;
    public string? InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO pro aktualizaci existujícího cvičení (Admin)
/// </summary>
public class UpdateInteractiveExerciseRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public string SolutionJson { get; set; } = string.Empty;
    public string? InstructionsMarkdown { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO pro validaci řešení od uživatele
/// </summary>
public class ValidateSolutionRequest
{
    public string UserSolutionJson { get; set; } = string.Empty;
}

/// <summary>
/// Response validace řešení
/// </summary>
public class ExerciseValidationResult
{
    public bool IsCorrect { get; set; }
    public int Score { get; set; } // 0-100
    public string Feedback { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public Dictionary<string, bool>? DetailedResults { get; set; } // Pro částečné hodnocení
}

/// <summary>
/// Response se správným řešením (pouze pro Admin)
/// </summary>
public class InteractiveExerciseSolutionResponse
{
    public int Id { get; set; }
    public string SolutionJson { get; set; } = string.Empty;
}
