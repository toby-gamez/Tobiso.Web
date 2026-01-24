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
            // Nejprve sestavíme seznam kategorií článku (včetně rodičů) pro dědičnost cvičení
            var post = await _context.Posts.Where(p => p.Id == postId).Select(p => new { p.Id, p.CategoryId }).FirstOrDefaultAsync();
            List<int> categoryIds = new();
            if (post != null && post.CategoryId.HasValue)
            {
                var current = post.CategoryId.Value;
                while (true)
                {
                    categoryIds.Add(current);
                    var parent = await _context.Categories.Where(c => c.Id == current).Select(c => c.ParentId).FirstOrDefaultAsync();
                    if (!parent.HasValue) break;
                    current = parent.Value;
                }
            }

            var query = _context.InteractiveExercises
                .Where(e => e.PostId == postId
                            || e.InteractiveExercisePosts.Any(p => p.PostId == postId)
                            || (categoryIds.Any() && e.InteractiveExerciseCategories.Any(ec => categoryIds.Contains(ec.CategoryId))));

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var exercises = await query
                .OrderBy(e => e.OrderIndex)
                .ToListAsync();

            var results = new List<InteractiveExerciseResponse>();
            foreach (var ex in exercises)
            {
                results.Add(await MapToResponseAsync(ex));
            }

            return results;
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

            return exercise != null ? await MapToResponseAsync(exercise) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání cvičení {Id}", id);
            throw;
        }
    }

    public async Task<List<InteractiveExerciseResponse>> GetAllAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.InteractiveExercises.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var exercises = await query.OrderBy(e => e.OrderIndex).ToListAsync();

            var results = new List<InteractiveExerciseResponse>();
            foreach (var ex in exercises)
            {
                results.Add(await MapToResponseAsync(ex));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání všech cvičení");
            throw;
        }
    }

    public async Task<InteractiveExerciseResponse> CreateAsync(CreateInteractiveExerciseRequest request)
    {
        try
        {
            // Validace JSON
            ValidateJson(request.ConfigJson, "ConfigJson");
            ValidateJson(request.SolutionJson, "SolutionJson");

            // Validate posts/categories if provided
            if (request.PostIds != null && request.PostIds.Any())
            {
                var missing = request.PostIds.Except(await _context.Posts.Where(p => request.PostIds.Contains(p.Id)).Select(p => p.Id).ToListAsync()).ToList();
                if (missing.Any()) throw new InvalidOperationException($"Neexistující články: {string.Join(',', missing)}");
            }
            if (request.CategoryIds != null && request.CategoryIds.Any())
            {
                var missingC = request.CategoryIds.Except(await _context.Categories.Where(c => request.CategoryIds.Contains(c.Id)).Select(c => c.Id).ToListAsync()).ToList();
                if (missingC.Any()) throw new InvalidOperationException($"Neexistující kategorie: {string.Join(',', missingC)}");
            }

            var exercise = new InteractiveExercise
            {
                // keep legacy PostId null or first if provided for compatibility
                PostId = request.PostIds != null && request.PostIds.Any() ? request.PostIds.First() : null,
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

            // Add join entries
            if (request.PostIds != null)
            {
                foreach (var pid in request.PostIds.Distinct())
                {
                    _context.InteractiveExercisePosts.Add(new InteractiveExercisePost { InteractiveExerciseId = exercise.Id, PostId = pid });
                }
            }
            if (request.CategoryIds != null)
            {
                foreach (var cid in request.CategoryIds.Distinct())
                {
                    _context.InteractiveExerciseCategories.Add(new InteractiveExerciseCategory { InteractiveExerciseId = exercise.Id, CategoryId = cid });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Vytvořeno cvičení {Title} (ID {Id})", exercise.Title, exercise.Id);

            return await MapToResponseAsync(exercise);
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

            // Validate posts/categories if provided
            if (request.PostIds != null && request.PostIds.Any())
            {
                var missing = request.PostIds.Except(await _context.Posts.Where(p => request.PostIds.Contains(p.Id)).Select(p => p.Id).ToListAsync()).ToList();
                if (missing.Any()) throw new InvalidOperationException($"Neexistující články: {string.Join(',', missing)}");
            }
            if (request.CategoryIds != null && request.CategoryIds.Any())
            {
                var missingC = request.CategoryIds.Except(await _context.Categories.Where(c => request.CategoryIds.Contains(c.Id)).Select(c => c.Id).ToListAsync()).ToList();
                if (missingC.Any()) throw new InvalidOperationException($"Neexistující kategorie: {string.Join(',', missingC)}");
            }

            exercise.Title = request.Title;
            exercise.Type = request.Type;
            exercise.ConfigJson = request.ConfigJson;
            exercise.SolutionJson = request.SolutionJson;
            exercise.InstructionsMarkdown = request.InstructionsMarkdown;
            exercise.OrderIndex = request.OrderIndex;
            exercise.IsActive = request.IsActive;
            exercise.UpdatedAt = DateTime.UtcNow;

            // Update join entries: posts
            if (request.PostIds != null)
            {
                var existing = _context.InteractiveExercisePosts.Where(x => x.InteractiveExerciseId == exercise.Id);
                _context.InteractiveExercisePosts.RemoveRange(existing);
                foreach (var pid in request.PostIds.Distinct())
                {
                    _context.InteractiveExercisePosts.Add(new InteractiveExercisePost { InteractiveExerciseId = exercise.Id, PostId = pid });
                }
            }

            // categories
            if (request.CategoryIds != null)
            {
                var existingC = _context.InteractiveExerciseCategories.Where(x => x.InteractiveExerciseId == exercise.Id);
                _context.InteractiveExerciseCategories.RemoveRange(existingC);
                foreach (var cid in request.CategoryIds.Distinct())
                {
                    _context.InteractiveExerciseCategories.Add(new InteractiveExerciseCategory { InteractiveExerciseId = exercise.Id, CategoryId = cid });
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Aktualizováno cvičení {Id}", exercise.Id);

            return await MapToResponseAsync(exercise);
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
        // kept for compatibility but use MapToResponseAsync in service methods when possible
        return new InteractiveExerciseResponse
        {
            Id = exercise.Id,
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

    private async Task<InteractiveExerciseResponse> MapToResponseAsync(InteractiveExercise exercise)
    {
        var postIds = await _context.InteractiveExercisePosts
            .Where(x => x.InteractiveExerciseId == exercise.Id)
            .Select(x => x.PostId)
            .ToListAsync();

        var categoryIds = await _context.InteractiveExerciseCategories
            .Where(x => x.InteractiveExerciseId == exercise.Id)
            .Select(x => x.CategoryId)
            .ToListAsync();

        return new InteractiveExerciseResponse
        {
            Id = exercise.Id,
            PostIds = postIds,
            CategoryIds = categoryIds,
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
        try
        {
            var userPairs = userSolution.RootElement.GetProperty("pairs");
            var correctPairs = correctSolution.RootElement.GetProperty("correctPairs");

            // Sestav slovníky leftId -> rightId
            var userDict = new Dictionary<string, string>();
            foreach (var pair in userPairs.EnumerateArray())
            {
                if (pair.TryGetProperty("leftId", out var left) && pair.TryGetProperty("rightId", out var right))
                {
                    userDict[left.GetString() ?? ""] = right.GetString() ?? "";
                }
            }
            var correctDict = new Dictionary<string, string>();
            foreach (var pair in correctPairs.EnumerateArray())
            {
                if (pair.TryGetProperty("leftId", out var left) && pair.TryGetProperty("rightId", out var right))
                {
                    correctDict[left.GetString() ?? ""] = right.GetString() ?? "";
                }
            }

            int correctCount = 0;
            int totalCount = correctDict.Count;
            foreach (var kv in correctDict)
            {
                if (userDict.TryGetValue(kv.Key, out var userRight))
                {
                    if (userRight == kv.Value)
                        correctCount++;
                }
            }

            var score = totalCount > 0 ? (int)((double)correctCount / totalCount * 100) : 0;
            var isCorrect = score == 100;

            return new ExerciseValidationResult
            {
                IsCorrect = isCorrect,
                Score = score,
                Feedback = isCorrect ? "Výborně! Všechny páry jsou správně!" : $"Správně {correctCount} z {totalCount} párů.",
                Explanation = isCorrect ? null : "Některé páry nejsou správně. Zkontroluj spojení."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při validaci matching");
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score = 0,
                Feedback = "Chyba při vyhodnocení."
            };
        }
    }
}
