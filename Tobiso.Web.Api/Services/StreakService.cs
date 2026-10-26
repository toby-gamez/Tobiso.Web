using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public record StreakInfo(int Current, int Longest, int Freezes, bool TodayActive, List<string> Days, List<string> FreezeDays);

public interface IStreakService
{
    /// <summary>Marks today (Europe/Prague) as active for the user. Idempotent.</summary>
    Task RecordActivityAsync(int userId);
    Task<StreakInfo> GetAsync(int userId);
}

public class StreakService(IDbContextFactory<TobisoDbContext> dbFactory) : IStreakService
{
    /// <summary>How many days back the stats payload lists.</summary>
    public const int WindowDays = 400;

    // Calendar days are Europe/Prague days (the audience and the app's local dates), not UTC.
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Europe/Prague", "Central Europe Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }

    public static DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone));

    public static DateOnly Today() => ToLocalDate(DateTime.UtcNow);

    public async Task RecordActivityAsync(int userId)
    {
        await using var db = dbFactory.CreateDbContext();
        var today = Today();
        if (await db.UserActivityDays.AnyAsync(a => a.UserId == userId && a.Date == today)) return;

        db.UserActivityDays.Add(new UserActivityDay { UserId = userId, Date = today });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Lost a race with a parallel call for the same day: the row exists now, which is the goal.
            if (!await db.UserActivityDays.AnyAsync(a => a.UserId == userId && a.Date == today)) throw;
        }
    }

    public async Task<StreakInfo> GetAsync(int userId)
    {
        await using var db = dbFactory.CreateDbContext();

        var activity = await db.UserActivityDays.AsNoTracking()
            .Where(a => a.UserId == userId).Select(a => a.Date).ToListAsync();
        var reads = await db.UserReadPosts.AsNoTracking()
            .Where(r => r.UserId == userId).Select(r => new { r.FirstReadAt, r.LastReadAt }).ToListAsync();
        var json = await db.UserGameStates.AsNoTracking()
            .Where(g => g.UserId == userId).Select(g => g.Json).FirstOrDefaultAsync();

        var blob = StreakCalculator.ParseBlob(json);
        var active = activity
            .Concat(reads.SelectMany(r => new[] { ToLocalDate(r.FirstReadAt), ToLocalDate(r.LastReadAt) }))
            .Concat(blob.Days);

        var today = Today();
        var result = StreakCalculator.Compute(active, blob.UsedFreezes, today);

        var cutoff = today.AddDays(-WindowDays);
        static string Fmt(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new StreakInfo(
            result.Current, result.Longest, blob.Freezes, result.TodayActive,
            [.. result.Days.Where(d => d >= cutoff).Select(Fmt)],
            [.. result.FreezeDays.Where(d => d >= cutoff).Select(Fmt)]);
    }
}
