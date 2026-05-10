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
                PostId = request.PostIds?.FirstOrDefault(),
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

        var user = JsonDocument.Parse(request.UserSolutionJson);
        var correct = JsonDocument.Parse(exercise.SolutionJson);

        return new ExerciseValidationResult
        {
            IsCorrect = user.RootElement.GetRawText() == correct.RootElement.GetRawText(),
            Score = 100,
            Feedback = "OK"
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
