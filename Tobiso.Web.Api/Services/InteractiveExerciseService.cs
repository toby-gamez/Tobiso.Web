using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Interfaces;

namespace Tobiso.Web.Api.Services;

public class InteractiveExerciseService : IInteractiveExerciseService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<InteractiveExerciseService> _logger;

    public InteractiveExerciseService(TobisoDbContext context, ILogger<InteractiveExerciseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<InteractiveExerciseResponse>> GetByPostIdAsync(int postId, bool includeInactive = false)
    {
        try
        {
            var post = await _context.Posts
                .Where(p => p.Id == postId)
                .Select(p => new { p.Id, p.CategoryId })
                .FirstOrDefaultAsync();

            // Load categories once and traverse in-memory to avoid N+1 roundtrips
            var categoryIds = new List<int>();
            if (post?.CategoryId != null)
            {
                var allCats = await _context.Categories
                    .Select(c => new { c.Id, c.ParentId })
                    .ToListAsync();
                var catDict = allCats.ToDictionary(c => c.Id, c => c.ParentId);

                var current = post.CategoryId.Value;
                var visited = new HashSet<int>();
                while (current != 0 && !visited.Contains(current))
                {
                    visited.Add(current);
                    categoryIds.Add(current);
                    if (!catDict.TryGetValue(current, out var parent) || parent == null)
                        break;
                    current = parent.Value;
                }
            }

            var exercises = await _context.InteractiveExercises
                .Where(e =>
                    e.PostId == postId ||
                    e.InteractiveExercisePosts.Any(p => p.PostId == postId) ||
                    (categoryIds.Any() &&
                     e.InteractiveExerciseCategories.Any(c => categoryIds.Contains(c.CategoryId))))
                .Where(e => includeInactive || e.IsActive)
                .OrderBy(e => e.OrderIndex)
                .Select(ex => new InteractiveExerciseResponse
                {
                    Id = ex.Id,
                    Title = ex.Title,
                    Type = ex.Type,
                    ConfigJson = ex.ConfigJson,
                    InstructionsMarkdown = ex.InstructionsMarkdown,
                    OrderIndex = ex.OrderIndex,
                    IsActive = ex.IsActive,
                    CreatedAt = ex.CreatedAt,
                    UpdatedAt = ex.UpdatedAt,

                    PostIds = ex.InteractiveExercisePosts
                        .Select(p => p.PostId)
                        .ToList(),

                    CategoryIds = ex.InteractiveExerciseCategories
                        .Select(c => c.CategoryId)
                        .ToList()
                })
                .ToListAsync();

            return exercises;
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
            return await _context.InteractiveExercises
                .Where(e => e.Id == id)
                .Select(ex => new InteractiveExerciseResponse
                {
                    Id = ex.Id,
                    Title = ex.Title,
                    Type = ex.Type,
                    ConfigJson = ex.ConfigJson,
                    InstructionsMarkdown = ex.InstructionsMarkdown,
                    OrderIndex = ex.OrderIndex,
                    IsActive = ex.IsActive,
                    CreatedAt = ex.CreatedAt,
                    UpdatedAt = ex.UpdatedAt,

                    PostIds = ex.InteractiveExercisePosts
                        .Select(p => p.PostId)
                        .ToList(),

                    CategoryIds = ex.InteractiveExerciseCategories
                        .Select(c => c.CategoryId)
                        .ToList()
                })
                .FirstOrDefaultAsync();
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
            return await _context.InteractiveExercises
                .Where(e => includeInactive || e.IsActive)
                .OrderBy(e => e.OrderIndex)
                .Select(ex => new InteractiveExerciseResponse
                {
                    Id = ex.Id,
                    Title = ex.Title,
                    Type = ex.Type,
                    ConfigJson = ex.ConfigJson,
                    InstructionsMarkdown = ex.InstructionsMarkdown,
                    OrderIndex = ex.OrderIndex,
                    IsActive = ex.IsActive,
                    CreatedAt = ex.CreatedAt,
                    UpdatedAt = ex.UpdatedAt,

                    PostIds = ex.InteractiveExercisePosts
                        .Select(p => p.PostId)
                        .ToList(),

                    CategoryIds = ex.InteractiveExerciseCategories
                        .Select(c => c.CategoryId)
                        .ToList()
                })
                .ToListAsync();
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
            ValidateJson(request.ConfigJson, "ConfigJson");
            ValidateJson(request.SolutionJson, "SolutionJson");

            var exercise = new InteractiveExercise
            {
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

            if (request.PostIds?.Any() == true)
            {
                foreach (var pid in request.PostIds.Distinct())
                {
                    _context.InteractiveExercisePosts.Add(new InteractiveExercisePost
                    {
                        InteractiveExerciseId = exercise.Id,
                        PostId = pid
                    });
                }
            }

            if (request.CategoryIds?.Any() == true)
            {
                foreach (var cid in request.CategoryIds.Distinct())
                {
                    _context.InteractiveExerciseCategories.Add(new InteractiveExerciseCategory
                    {
                        InteractiveExerciseId = exercise.Id,
                        CategoryId = cid
                    });
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(exercise.Id)
                   ?? throw new Exception("Failed to load created exercise");
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
                return null;

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

            // reset relations
            _context.InteractiveExercisePosts.RemoveRange(
                _context.InteractiveExercisePosts.Where(x => x.InteractiveExerciseId == exercise.Id));

            _context.InteractiveExerciseCategories.RemoveRange(
                _context.InteractiveExerciseCategories.Where(x => x.InteractiveExerciseId == exercise.Id));

            if (request.PostIds?.Any() == true)
            {
                foreach (var pid in request.PostIds.Distinct())
                {
                    _context.InteractiveExercisePosts.Add(new InteractiveExercisePost
                    {
                        InteractiveExerciseId = exercise.Id,
                        PostId = pid
                    });
                }
            }

            if (request.CategoryIds?.Any() == true)
            {
                foreach (var cid in request.CategoryIds.Distinct())
                {
                    _context.InteractiveExerciseCategories.Add(new InteractiveExerciseCategory
                    {
                        InteractiveExerciseId = exercise.Id,
                        CategoryId = cid
                    });
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(exercise.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci cvičení {Id}", request.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var exercise = await _context.InteractiveExercises.FindAsync(id);

        if (exercise == null)
            return false;

        _context.InteractiveExercises.Remove(exercise);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ExerciseValidationResult> ValidateSolutionAsync(int exerciseId, ValidateSolutionRequest request)
    {
        var exercise = await _context.InteractiveExercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId)
            ?? throw new InvalidOperationException("Exercise not found");

        using var userDoc    = JsonDocument.Parse(request.UserSolutionJson);
        using var solutionDoc = JsonDocument.Parse(exercise.SolutionJson);

        var userRoot     = userDoc.RootElement;
        var solutionRoot = solutionDoc.RootElement;

        // Pull the shared optional explanation out of the solution JSON once.
        var explanation = solutionRoot.TryGetProperty("explanation", out var expProp)
            ? expProp.GetString()
            : null;

        return exercise.Type switch
        {
            ExerciseTypeConstants.Timeline => ValidateTimeline(userRoot, solutionRoot, explanation),
            ExerciseTypeConstants.DragDrop => ValidateDragDrop(userRoot, solutionRoot, explanation),
            ExerciseTypeConstants.Matching => ValidateMatching(userRoot, solutionRoot, explanation),
            // Circuit / Molecule / unknown: fall back to raw-JSON equality.
            _ => ValidateFallback(userRoot, solutionRoot, explanation)
        };
    }

    // ── Timeline ─────────────────────────────────────────────────────────────
    // User    : { "order": ["event-1", "event-2", ...] }
    // Solution: { "correctOrder": ["event-1", "event-2", ...], "explanation": "..." }
    private static ExerciseValidationResult ValidateTimeline(
        JsonElement user, JsonElement solution, string? explanation)
    {
        if (!user.TryGetProperty("order", out var userOrderEl)
            || !solution.TryGetProperty("correctOrder", out var correctOrderEl))
        {
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score     = 0,
                Feedback  = "Chybný formát odpovědi.",
                Explanation = explanation
            };
        }

        var userOrder    = userOrderEl.EnumerateArray().Select(e => e.GetString()).ToList();
        var correctOrder = correctOrderEl.EnumerateArray().Select(e => e.GetString()).ToList();

        bool isCorrect = userOrder.SequenceEqual(correctOrder);
        return new ExerciseValidationResult
        {
            IsCorrect   = isCorrect,
            Score       = isCorrect ? 100 : 0,
            Feedback    = isCorrect ? "Správně! Pořadí událostí je přesné." : "Pořadí událostí není správné. Zkuste to znovu.",
            Explanation = explanation
        };
    }

    // ── Drag-drop ─────────────────────────────────────────────────────────────
    // User    : { "placements": { "itemId": "categoryId", ... } }
    // Solution: { "correctPlacements": { "itemId": "categoryId", ... }, "explanation": "..." }
    private static ExerciseValidationResult ValidateDragDrop(
        JsonElement user, JsonElement solution, string? explanation)
    {
        if (!user.TryGetProperty("placements", out var userPlacementsEl)
            || !solution.TryGetProperty("correctPlacements", out var correctPlacementsEl))
        {
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score     = 0,
                Feedback  = "Chybný formát odpovědi.",
                Explanation = explanation
            };
        }

        var correct = correctPlacementsEl.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);

        var detailed = new Dictionary<string, bool>();
        int correctCount = 0;

        foreach (var prop in userPlacementsEl.EnumerateObject())
        {
            var userVal    = prop.Value.GetString() ?? string.Empty;
            bool itemOk    = correct.TryGetValue(prop.Name, out var expected) && expected == userVal;
            detailed[prop.Name] = itemOk;
            if (itemOk) correctCount++;
        }

        int total     = correct.Count;
        int score     = total == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / total);
        bool isCorrect = score == 100;

        string feedback = isCorrect
            ? "Výborně! Vše správně umístěno."
            : $"Správně jste umístil(a) {correctCount} z {total} položek. Zkuste to znovu.";

        return new ExerciseValidationResult
        {
            IsCorrect       = isCorrect,
            Score           = score,
            Feedback        = feedback,
            Explanation     = explanation,
            DetailedResults = detailed
        };
    }

    // ── Matching ─────────────────────────────────────────────────────────────
    // User    : { "pairs": [{ "leftId": "l-1", "rightId": "r-1" }, ...] }
    // Solution: { "pairs": [{ "id": "pair-1", "leftId": "l-1", "rightId": "r-1" }, ...], "explanation": "..." }
    private static ExerciseValidationResult ValidateMatching(
        JsonElement user, JsonElement solution, string? explanation)
    {
        if (!user.TryGetProperty("pairs", out var userPairsEl)
            || !solution.TryGetProperty("pairs", out var solutionPairsEl))
        {
            return new ExerciseValidationResult
            {
                IsCorrect = false,
                Score     = 0,
                Feedback  = "Chybný formát odpovědi.",
                Explanation = explanation
            };
        }

        // Build a lookup: leftId → correct rightId
        var correctMap = solutionPairsEl.EnumerateArray()
            .ToDictionary(
                p => p.GetProperty("leftId").GetString() ?? string.Empty,
                p => p.GetProperty("rightId").GetString() ?? string.Empty);

        var detailed = new Dictionary<string, bool>();
        int correctCount = 0;

        foreach (var pair in userPairsEl.EnumerateArray())
        {
            var leftId  = pair.TryGetProperty("leftId",  out var lProp) ? lProp.GetString() ?? string.Empty : string.Empty;
            var rightId = pair.TryGetProperty("rightId", out var rProp) ? rProp.GetString() ?? string.Empty : string.Empty;

            bool pairOk = correctMap.TryGetValue(leftId, out var expectedRight) && expectedRight == rightId;
            detailed[leftId] = pairOk;
            if (pairOk) correctCount++;
        }

        int total      = correctMap.Count;
        int score      = total == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / total);
        bool isCorrect = score == 100;

        string feedback = isCorrect
            ? "Výborně! Všechny páry jsou správně spojeny."
            : $"Správně jste spojil(a) {correctCount} z {total} párů. Zkuste to znovu.";

        return new ExerciseValidationResult
        {
            IsCorrect       = isCorrect,
            Score           = score,
            Feedback        = feedback,
            Explanation     = explanation,
            DetailedResults = detailed
        };
    }

    // ── Fallback (circuit / molecule / unknown) ────────────────────────────────
    private static ExerciseValidationResult ValidateFallback(
        JsonElement user, JsonElement solution, string? explanation)
    {
        bool isCorrect = user.GetRawText() == solution.GetRawText();
        return new ExerciseValidationResult
        {
            IsCorrect   = isCorrect,
            Score       = isCorrect ? 100 : 0,
            Feedback    = isCorrect ? "Správně!" : "Řešení není správné. Zkuste to znovu.",
            Explanation = explanation
        };
    }

    public async Task<InteractiveExerciseSolutionResponse?> GetSolutionAsync(int id)
    {
        return await _context.InteractiveExercises
            .Where(e => e.Id == id)
            .Select(e => new InteractiveExerciseSolutionResponse
            {
                Id = e.Id,
                SolutionJson = e.SolutionJson
            })
            .FirstOrDefaultAsync();
    }

    private static void ValidateJson(string json, string name)
    {
        JsonDocument.Parse(json);
    }
}
