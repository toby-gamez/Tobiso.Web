using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Interfaces;
using System.Text.Json;

namespace Tobiso.Web.Api.Services;

/// <summary>
/// Service pro správu interaktivních cvičení
/// </summary>
public class InteractiveExerciseService : IInteractiveExerciseService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<InteractiveExerciseService> _logger;

    public InteractiveExerciseService(TobisoDbContext context, ILogger<InteractiveExerciseService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<InteractiveExerciseResponse>> GetByPostIdAsync(int postId, bool includeInactive = false)
    {
        try
        {
            var query = _context.InteractiveExercises
                .Where(e => e.PostId == postId);

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var exercises = await query
                .OrderBy(e => e.OrderIndex)
                .ToListAsync();

            return exercises.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání cvičení pro článek {PostId}", postId);
            throw;
        }
    }

    public async Task<InteractiveExerciseResponse?> GetByIdAsync(int id)
    {
        try
        {
            var exercise = await _context.InteractiveExercises
                .FirstOrDefaultAsync(e => e.Id == id);

            return exercise != null ? MapToResponse(exercise) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání cvičení {Id}", id);
            throw;
        }
    }

    public async Task<InteractiveExerciseResponse> CreateAsync(CreateInteractiveExerciseRequest request)
    {
        try
        {
            // Validace: zkontroluj, že Post existuje
            var postExists = await _context.Posts.AnyAsync(p => p.Id == request.PostId);
            if (!postExists)
            {
                throw new InvalidOperationException($"Článek s ID {request.PostId} neexistuje.");
            }

            // Validace JSON
            ValidateJson(request.ConfigJson, "ConfigJson");
            ValidateJson(request.SolutionJson, "SolutionJson");

            var exercise = new InteractiveExercise
            {
                PostId = request.PostId,
                Title = request.Title,
                Type = request.Type,
                ConfigJson = request.ConfigJson,
                SolutionJson = request.SolutionJson,
                InstructionsMarkdown = request.InstructionsMarkdown,
                OrderIndex = request.OrderIndex,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.InteractiveExercises.Add(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vytvořeno cvičení {Title} pro článek {PostId}", exercise.Title, exercise.PostId);

            return MapToResponse(exercise);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření cvičení");
            throw;
        }
    }

    public async Task<InteractiveExerciseResponse?> UpdateAsync(UpdateInteractiveExerciseRequest request)
    {
        try
        {
            var exercise = await _context.InteractiveExercises
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (exercise == null)
            {
                _logger.LogWarning("Cvičení s ID {Id} nenalezeno", request.Id);
                return null;
            }

            // Validace JSON
            ValidateJson(request.ConfigJson, "ConfigJson");
            ValidateJson(request.SolutionJson, "SolutionJson");

            exercise.Title = request.Title;
            exercise.Type = request.Type;
            exercise.ConfigJson = request.ConfigJson;
            exercise.SolutionJson = request.SolutionJson;
            exercise.InstructionsMarkdown = request.InstructionsMarkdown;
            exercise.OrderIndex = request.OrderIndex;
            exercise.IsActive = request.IsActive;
            exercise.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Aktualizováno cvičení {Id}", exercise.Id);

            return MapToResponse(exercise);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci cvičení {Id}", request.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var exercise = await _context.InteractiveExercises
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise == null)
            {
                _logger.LogWarning("Cvičení s ID {Id} nenalezeno", id);
                return false;
            }

            _context.InteractiveExercises.Remove(exercise);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Smazáno cvičení {Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání cvičení {Id}", id);
            throw;
        }
    }

    public async Task<ExerciseValidationResult> ValidateSolutionAsync(int exerciseId, ValidateSolutionRequest request)
    {
        try
        {
            var exercise = await _context.InteractiveExercises
                .FirstOrDefaultAsync(e => e.Id == exerciseId);

            if (exercise == null)
            {
                throw new InvalidOperationException($"Cvičení s ID {exerciseId} neexistuje.");
            }

            // Parse JSONů
            var userSolution = JsonDocument.Parse(request.UserSolutionJson);
            var correctSolution = JsonDocument.Parse(exercise.SolutionJson);

            // Validace podle typu cvičení
            var result = exercise.Type switch
            {
                "circuit" => ValidateCircuit(userSolution, correctSolution),
                "timeline" => ValidateTimeline(userSolution, correctSolution),
                "drag-drop" => ValidateDragDrop(userSolution, correctSolution),
                "molecule" => ValidateMolecule(userSolution, correctSolution),
                "matching" => ValidateMatching(userSolution, correctSolution),
                _ => new ExerciseValidationResult
                {
                    IsCorrect = false,
                    Score = 0,
                    Feedback = "Neznámý typ cvičení."
                }
            };

            _logger.LogInformation("Validace cvičení {Id}: skóre {Score}%", exerciseId, result.Score);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci řešení cvičení {Id}", exerciseId);
            throw;
        }
    }

    public async Task<InteractiveExerciseSolutionResponse?> GetSolutionAsync(int id)
    {
        try
        {
            var exercise = await _context.InteractiveExercises
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise == null)
            {
                return null;
            }

            return new InteractiveExerciseSolutionResponse
            {
                Id = exercise.Id,
                SolutionJson = exercise.SolutionJson
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání řešení cvičení {Id}", id);
            throw;
        }
    }

    // === Helper Methods ===

    private static InteractiveExerciseResponse MapToResponse(InteractiveExercise exercise)
    {
        return new InteractiveExerciseResponse
        {
            Id = exercise.Id,
            PostId = exercise.PostId,
            Title = exercise.Title,
            Type = exercise.Type,
            ConfigJson = exercise.ConfigJson,
            InstructionsMarkdown = exercise.InstructionsMarkdown,
            OrderIndex = exercise.OrderIndex,
            IsActive = exercise.IsActive,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };
    }

    private void ValidateJson(string json, string fieldName)
    {
        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Nevalidní JSON v poli {fieldName}: {ex.Message}");
        }
    }

    // === Validační metody pro jednotlivé typy cvičení ===

    private ExerciseValidationResult ValidateCircuit(JsonDocument userSolution, JsonDocument correctSolution)
    {
        try
        {
            var userConnections = userSolution.RootElement.GetProperty("connections");
            var correctConnections = correctSolution.RootElement.GetProperty("correctConnections");

            var userSet = new HashSet<string>();
            var correctSet = new HashSet<string>();

            foreach (var conn in userConnections.EnumerateArray())
            {
                var from = conn.GetProperty("from").GetString();
                var to = conn.GetProperty("to").GetString();
                userSet.Add($"{from}:{to}");
            }

            foreach (var conn in correctConnections.EnumerateArray())
            {
                var from = conn.GetProperty("from").GetString();
                var to = conn.GetProperty("to").GetString();
                correctSet.Add($"{from}:{to}");
            }

            var correctCount = userSet.Intersect(correctSet).Count();
            var score = (int)((double)correctCount / correctSet.Count * 100);
            var isCorrect = score == 100;

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                Score = score,
                Feedback = isCorrect ? "Výborně! Obvod je správně zapojen." : $"Máš správně {correctCount} z {correctSet.Count} spojení.",
                Explanation = isCorrect ? null : "Zkontroluj zapojení komponent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci obvodu");
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score = 0,
                Feedback = "Chyba při vyhodnocení."
            };
        }
    }

    private ExerciseValidationResult ValidateTimeline(JsonDocument userSolution, JsonDocument correctSolution)
    {
        try
        {
            var userOrder = new List<string>();
            var correctOrder = new List<string>();

            foreach (var item in userSolution.RootElement.GetProperty("order").EnumerateArray())
            {
                userOrder.Add(item.GetString()!);
            }

            foreach (var item in correctSolution.RootElement.GetProperty("correctOrder").EnumerateArray())
            {
                correctOrder.Add(item.GetString()!);
            }

            var isCorrect = userOrder.SequenceEqual(correctOrder);
            var correctCount = userOrder.Zip(correctOrder, (a, b) => a == b).Count(x => x);
            var score = (int)((double)correctCount / correctOrder.Count * 100);

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                Score = score,
                Feedback = isCorrect ? "Perfektní! Časová osa je správně." : $"Správně {correctCount} z {correctOrder.Count} událostí.",
                Explanation = isCorrect ? null : "Zkontroluj chronologické pořadí."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci časové osy");
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score = 0,
                Feedback = "Chyba při vyhodnocení."
            };
        }
    }

    private ExerciseValidationResult ValidateDragDrop(JsonDocument userSolution, JsonDocument correctSolution)
    {
        try
        {
            var userPlacements = userSolution.RootElement.GetProperty("placements");
            var correctPlacements = correctSolution.RootElement.GetProperty("correctPlacements");

            var correctCount = 0;
            var totalCount = 0;

            foreach (var correct in correctPlacements.EnumerateObject())
            {
                totalCount++;
                if (userPlacements.TryGetProperty(correct.Name, out var userValue))
                {
                    if (userValue.GetString() == correct.Value.GetString())
                    {
                        correctCount++;
                    }
                }
            }

            var score = (int)((double)correctCount / totalCount * 100);
            var isCorrect = score == 100;

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                Score = score,
                Feedback = isCorrect ? "Skvělé! Všechna slova jsou správně zařazena." : $"Správně {correctCount} z {totalCount} slov.",
                Explanation = isCorrect ? null : "Některá slova nejsou ve správné kategorii."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci drag-drop");
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score = 0,
                Feedback = "Chyba při vyhodnocení."
            };
        }
    }

    private ExerciseValidationResult ValidateMolecule(JsonDocument userSolution, JsonDocument correctSolution)
    {
        // Podobně jako circuit - validace atomů a vazeb
        try
        {
            var userAtoms = userSolution.RootElement.GetProperty("atoms");
            var correctAtoms = correctSolution.RootElement.GetProperty("correctAtoms");

            // Zjednodušená validace - v reálu by se porovnávaly struktury
            var userJson = userSolution.RootElement.GetRawText();
            var correctJson = correctSolution.RootElement.GetRawText();

            var isCorrect = userJson == correctJson;
            var score = isCorrect ? 100 : 50; // Částečné body za pokus

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                Score = score,
                Feedback = isCorrect ? "Výborně! Molekula je správně sestavena." : "Molekula není úplně správně.",
                Explanation = isCorrect ? null : "Zkontroluj počet a typ atomů a vazeb."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci molekuly");
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score = 0,
                Feedback = "Chyba při vyhodnocení."
            };
        }
    }

    private ExerciseValidationResult ValidateMatching(JsonDocument userSolution, JsonDocument correctSolution)
    {
        // Podobně jako drag-drop - párování prvků
        return ValidateDragDrop(userSolution, correctSolution);
    }
}
