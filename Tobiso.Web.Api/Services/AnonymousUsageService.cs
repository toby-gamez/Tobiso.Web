using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAnonymousUsageService
{
    Task<bool> TryConsumeAsync(string deviceId, int limit);
    Task<int> GetRemainingAsync(string deviceId, int limit);
}

// Backs the anonymous free-AI allowance with a persistent, per-device daily counter: "N requests
// per UTC day," surviving app restarts (unlike the in-memory per-user daily counter in
// AiRateLimitService) and not shared across everyone on the same IP/NAT. The counter resets itself
// the first time a device is seen on a new UTC day - there's no background job, the reset is just
// part of the same conditional UPDATE that consumes a request.
public class AnonymousUsageService : IAnonymousUsageService
{
    private readonly TobisoDbContext _db;

    public AnonymousUsageService(TobisoDbContext db) => _db = db;

    public async Task<bool> TryConsumeAsync(string deviceId, int limit)
    {
        if (!await _db.AnonymousAiUsages.AnyAsync(a => a.DeviceId == deviceId))
        {
            try
            {
                _db.AnonymousAiUsages.Add(new AnonymousAiUsage { DeviceId = deviceId, Count = 0 });
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another concurrent request for the same device inserted first - fine, the
                // unique index on DeviceId means exactly one row exists either way.
                _db.ChangeTracker.Clear();
            }
        }

        // A single conditional UPDATE so two concurrent requests for the same device can't both
        // read Count=19, both pass an in-memory check, and both consume - running the device past
        // its allowance. Also handles the daily reset: a device last used on an earlier UTC day is
        // always allowed through (WHERE) and its count restarts at 1 instead of incrementing (SET),
        // rather than needing a separate reset step before the increment.
        var today = DateTime.UtcNow.Date;
        var rows = await _db.AnonymousAiUsages
            .Where(a => a.DeviceId == deviceId && (a.LastUsedAt.Date < today || a.Count < limit))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Count, a => a.LastUsedAt.Date < today ? 1 : a.Count + 1)
                .SetProperty(a => a.LastUsedAt, DateTime.UtcNow));
        return rows > 0;
    }

    public async Task<int> GetRemainingAsync(string deviceId, int limit)
    {
        var row = await _db.AnonymousAiUsages
            .Where(a => a.DeviceId == deviceId)
            .Select(a => new { a.Count, a.LastUsedAt })
            .FirstOrDefaultAsync();
        if (row == null) return limit;
        var count = row.LastUsedAt.Date < DateTime.UtcNow.Date ? 0 : row.Count;
        return Math.Max(0, limit - count);
    }
}
