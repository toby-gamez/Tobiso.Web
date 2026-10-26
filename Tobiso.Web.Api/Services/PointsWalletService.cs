using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Helpers;

namespace Tobiso.Web.Api.Services;

public enum PointsClaimStatus { Ok, UnknownItem, InsufficientPoints, DailyLimit }

public interface IPointsWalletService
{
    Task<PointsWalletDto> GetAsync(int userId);
    /// <summary>Grades the answers against the stored questions, records the attempts and awards points.</summary>
    Task<PointsEarnResultDto> GradeAndAwardAsync(int userId, IReadOnlyList<QuizAnswerSubmission> answers);
    /// <summary>Awards points for answers whose correctness the caller already determined server-side.</summary>
    Task<PointsEarnResultDto> AwardAsync(int userId, IEnumerable<(int QuestionId, bool Correct)> results);
    Task<(PointsClaimStatus Status, PointsClaimResultDto? Result)> ClaimAsync(int userId, string reason, string reference);
}

// The only source of spendable points. The client never states an amount or a balance: it only submits
// answers, the server grades them, and each (question, UTC day) pays out once, up to a daily cap.
public class PointsWalletService(IDbContextFactory<TobisoDbContext> dbFactory, IQuestionAttemptService attempts)
    : IPointsWalletService
{
    public const int PointsPerCorrectAnswer = 2;
    public const int DailyEarnCap = 150;
    public const int MaxAnswersPerRequest = 50;
    private const string QuizKind = "quiz";
    private const string ExchangeKind = "exchange";
    private const string ExchangeReason = "points_exchange";
    private const int MaxExchangedCreditsPerDay = 20;

    // Server-side price list for the points -> AI credits exchange, keyed by the app's shop item id.
    private static readonly Dictionary<string, (int Cost, int Credits)> AiCreditPacks = new()
    {
        ["80"] = (40, 5),
        ["81"] = (70, 10),
    };

    public async Task<PointsWalletDto> GetAsync(int userId)
    {
        await using var db = dbFactory.CreateDbContext();
        var balance = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.PointsBalance).FirstOrDefaultAsync();
        return new PointsWalletDto(balance, await EarnedTodayAsync(db, userId), DailyEarnCap);
    }

    public async Task<PointsEarnResultDto> GradeAndAwardAsync(int userId, IReadOnlyList<QuizAnswerSubmission> answers)
    {
        var distinct = answers.GroupBy(a => a.QuestionId).Select(g => g.First()).Take(MaxAnswersPerRequest).ToList();
        var ids = distinct.Select(a => a.QuestionId).ToList();

        List<Question> questions;
        await using (var db = dbFactory.CreateDbContext())
            questions = await db.Questions.AsNoTracking().Include(q => q.Answers).Where(q => ids.Contains(q.Id)).ToListAsync();
        var byId = questions.ToDictionary(q => q.Id);

        var results = new List<(int, bool)>();
        foreach (var a in distinct)
        {
            if (!byId.TryGetValue(a.QuestionId, out var q)) continue;
            var correct = IsCorrect(q, a);
            results.Add((q.Id, correct));
            await attempts.RecordAttemptAsync(userId, q.Id, correct);
        }
        return await AwardAsync(userId, results);
    }

    public async Task<PointsEarnResultDto> AwardAsync(int userId, IEnumerable<(int QuestionId, bool Correct)> results)
    {
        await using var db = dbFactory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        var stamp = today.ToString("yyyyMMdd");
        var correctIds = results.Where(r => r.Correct).Select(r => r.QuestionId).Distinct().ToList();
        var refs = correctIds.Select(id => $"{id}:{stamp}").ToList();

        var already = await db.PointsLedger.AsNoTracking()
            .Where(e => e.UserId == userId && e.Kind == QuizKind && e.Ref != null && refs.Contains(e.Ref))
            .Select(e => e.Ref!).ToListAsync();
        var fresh = refs.Except(already).ToList();

        var earnedToday = await EarnedTodayAsync(db, userId);
        var room = Math.Max(0, DailyEarnCap - earnedToday);
        var payable = Math.Min(fresh.Count, room / PointsPerCorrectAnswer);
        var awarded = 0;

        if (payable > 0)
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            foreach (var r in fresh.Take(payable))
                db.PointsLedger.Add(new PointsLedgerEntry { UserId = userId, Delta = PointsPerCorrectAnswer, Kind = QuizKind, Ref = r });
            try
            {
                await db.SaveChangesAsync();
                awarded = payable * PointsPerCorrectAnswer;
                await db.Users.Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.PointsBalance, u => u.PointsBalance + awarded));
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent request already paid one of these answers (unique index); nothing is awarded, the client may retry.
                awarded = 0;
            }
        }

        var balance = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.PointsBalance).FirstOrDefaultAsync();
        return new PointsEarnResultDto(correctIds.Count, awarded, balance, earnedToday + awarded, DailyEarnCap);
    }

    public async Task<(PointsClaimStatus, PointsClaimResultDto?)> ClaimAsync(int userId, string reason, string reference)
    {
        if (reason != "ai_credits" || !AiCreditPacks.TryGetValue(reference, out var pack))
            return (PointsClaimStatus.UnknownItem, null);

        await using var db = dbFactory.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();

        var today = DateTime.UtcNow.Date;
        var exchangedToday = await db.AiCreditTransactions
            .Where(t => t.UserId == userId && t.Reason == ExchangeReason && t.CreatedAt >= today)
            .SumAsync(t => (int?)t.Delta) ?? 0;
        if (exchangedToday + pack.Credits > MaxExchangedCreditsPerDay) return (PointsClaimStatus.DailyLimit, null);

        // Atomic compare-and-debit: concurrent claims can't overspend the balance.
        var debited = await db.Users
            .Where(u => u.Id == userId && u.PointsBalance >= pack.Cost)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PointsBalance, u => u.PointsBalance - pack.Cost)
                .SetProperty(u => u.Credits, u => u.Credits + pack.Credits));
        if (debited == 0) return (PointsClaimStatus.InsufficientPoints, null);

        db.PointsLedger.Add(new PointsLedgerEntry { UserId = userId, Delta = -pack.Cost, Kind = ExchangeKind });
        db.AiCreditTransactions.Add(new AiCreditTransaction { UserId = userId, Delta = pack.Credits, Reason = ExchangeReason });
        await db.SaveChangesAsync();
        var row = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.Credits, u.PointsBalance }).FirstAsync();
        await tx.CommitAsync();

        return (PointsClaimStatus.Ok, new PointsClaimResultDto(row.Credits, row.PointsBalance));
    }

    private static async Task<int> EarnedTodayAsync(TobisoDbContext db, int userId)
    {
        var today = DateTime.UtcNow.Date;
        return await db.PointsLedger.AsNoTracking()
            .Where(e => e.UserId == userId && e.Delta > 0 && e.CreatedAt >= today)
            .SumAsync(e => (int?)e.Delta) ?? 0;
    }

    // Same rules as the web quiz: a single-answer question is a free-text prompt (diacritics- and
    // case-insensitive); otherwise the picked ids must equal the set of correct answers exactly.
    private static bool IsCorrect(Question q, QuizAnswerSubmission a)
    {
        if (q.Answers.Count == 1)
        {
            if (string.IsNullOrWhiteSpace(a.Text)) return false;
            return TextSearch.RemoveDiacritics(a.Text.Trim())
                .Equals(TextSearch.RemoveDiacritics(q.Answers[0].AnswerText.Trim()), StringComparison.OrdinalIgnoreCase);
        }
        var picked = (a.AnswerIds ?? []).ToHashSet();
        var correct = q.Answers.Where(x => x.Correct == 1).Select(x => x.Id).ToHashSet();
        return picked.Count > 0 && picked.SetEquals(correct);
    }
}
