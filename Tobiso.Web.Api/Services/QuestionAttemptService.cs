using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IQuestionAttemptService
{
    Task RecordAttemptAsync(int userId, int questionId, bool isCorrect);
    Task<PracticeStatsDto> GetStatsAsync(int userId);
    Task<List<int>> GetWeakCategoryIds(int userId, int take = 3);
    Task<List<QuestionResponse>> GetSessionAsync(int? userId, List<int>? categoryIds, int count, bool preferUnmastered);
}

public class QuestionAttemptService : IQuestionAttemptService
{
    private readonly TobisoDbContext _db;
    private readonly IQuestionService _questionService;

    public QuestionAttemptService(TobisoDbContext db, IQuestionService questionService)
    {
        _db = db;
        _questionService = questionService;
    }

    public async Task RecordAttemptAsync(int userId, int questionId, bool isCorrect)
    {
        var record = await _db.QuestionAttempts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.QuestionId == questionId);

        if (record == null)
        {
            _db.QuestionAttempts.Add(new QuestionAttempt
            {
                UserId = userId,
                QuestionId = questionId,
                LastCorrect = isCorrect,
                TimesCorrect = isCorrect ? 1 : 0,
                TimesWrong = isCorrect ? 0 : 1,
                FirstAttemptedAt = DateTime.UtcNow,
                LastAttemptedAt = DateTime.UtcNow
            });
        }
        else
        {
            record.LastCorrect = isCorrect;
            if (isCorrect) record.TimesCorrect++; else record.TimesWrong++;
            record.LastAttemptedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PracticeStatsDto> GetStatsAsync(int userId)
    {
        var totalQuestions = await _db.Questions.CountAsync();
        var attempts = await _db.QuestionAttempts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var totalCorrect = attempts.Sum(a => a.TimesCorrect);
        var totalAnswers = attempts.Sum(a => a.TimesCorrect + a.TimesWrong);

        return new PracticeStatsDto
        {
            TotalQuestions = totalQuestions,
            AttemptedCount = attempts.Count,
            MasteredCount = attempts.Count(a => a.LastCorrect && a.TimesCorrect >= 2),
            AccuracyPercent = totalAnswers == 0 ? 0 : Math.Round(100.0 * totalCorrect / totalAnswers, 1)
        };
    }

    public async Task<List<int>> GetWeakCategoryIds(int userId, int take = 3)
    {
        var rows = await _db.QuestionAttempts
            .Where(a => a.UserId == userId)
            .Select(a => new { a.TimesCorrect, a.TimesWrong, CategoryId = a.Question.Post!.CategoryId })
            .Where(x => x.CategoryId != null)
            .ToListAsync();

        return rows
            .GroupBy(x => x.CategoryId!.Value)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(x => x.TimesCorrect + x.TimesWrong),
                Correct = g.Sum(x => x.TimesCorrect)
            })
            .Where(x => x.Total >= 3)
            .OrderBy(x => (double)x.Correct / x.Total)
            .Take(take)
            .Select(x => x.CategoryId)
            .ToList();
    }

    public async Task<List<QuestionResponse>> GetSessionAsync(int? userId, List<int>? categoryIds, int count, bool preferUnmastered)
    {
        var query = _db.Questions.AsNoTracking().AsQueryable();
        if (categoryIds is { Count: > 0 })
        {
            query = query.Where(q => q.Post != null && q.Post.CategoryId != null
                && categoryIds.Contains(q.Post.CategoryId.Value));
        }

        var candidateIds = await query.Select(q => q.Id).ToListAsync();
        if (candidateIds.Count == 0) return new List<QuestionResponse>();

        var take = Math.Clamp(count <= 0 ? 25 : count, 1, 50);
        var rng = new Random();
        List<int> orderedIds;

        if (userId.HasValue && preferUnmastered)
        {
            var attempts = await _db.QuestionAttempts
                .Where(a => a.UserId == userId.Value && candidateIds.Contains(a.QuestionId))
                .ToDictionaryAsync(a => a.QuestionId);

            bool IsMastered(int id) => attempts.TryGetValue(id, out var a) && a.LastCorrect && a.TimesCorrect >= 2;

            var unmastered = candidateIds.Where(id => !IsMastered(id)).OrderBy(_ => rng.Next()).ToList();
            var mastered = candidateIds.Where(IsMastered).OrderBy(_ => rng.Next()).ToList();
            orderedIds = unmastered.Concat(mastered).Take(take).ToList();
        }
        else
        {
            orderedIds = candidateIds.OrderBy(_ => rng.Next()).Take(take).ToList();
        }

        var questions = await _questionService.GetByIds(orderedIds);
        var order = orderedIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
        return questions.OrderBy(q => order[q.Id]).ToList();
    }
}
