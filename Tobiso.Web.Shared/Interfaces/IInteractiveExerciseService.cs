using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Shared.Interfaces;

/// <summary>
/// Service interface pro správu interaktivních cvičení
/// </summary>
public interface IInteractiveExerciseService
{
    /// <summary>
    /// Získá všechna cvičení pro daný článek
    /// </summary>
    Task<List<InteractiveExerciseResponse>> GetByPostIdAsync(int postId, bool includeInactive = false);
    
    /// <summary>
    /// Získá konkrétní cvičení podle ID
    /// </summary>
    Task<InteractiveExerciseResponse?> GetByIdAsync(int id);
    
    /// <summary>
    /// Vytvoří nové cvičení (Admin)
    /// </summary>
    Task<InteractiveExerciseResponse> CreateAsync(CreateInteractiveExerciseRequest request);
    
    /// <summary>
    /// Aktualizuje existující cvičení (Admin)
    /// </summary>
    Task<InteractiveExerciseResponse?> UpdateAsync(UpdateInteractiveExerciseRequest request);
    
    /// <summary>
    /// Smaže cvičení (Admin)
    /// </summary>
    Task<bool> DeleteAsync(int id);
    
    /// <summary>
    /// Validuje řešení od uživatele
    /// </summary>
    Task<ExerciseValidationResult> ValidateSolutionAsync(int exerciseId, ValidateSolutionRequest request);
    
    /// <summary>
    /// Získá správné řešení (pouze Admin)
    /// </summary>
    Task<InteractiveExerciseSolutionResponse?> GetSolutionAsync(int id);
}
