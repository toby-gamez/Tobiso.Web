using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAnonymousUsageService
{
    Task<bool> TryConsumeAsync(string deviceId, int limit);
    Task<int> GetRemainingAsync(string deviceId, int limit);
}

// Backs the anonymous free-AI allowance with a persistent, per-device counter that never resets
// (unlike the old in-memory per-IP daily counter): "N requests, ever," surviving app restarts and
// not shared across everyone on the same IP/NAT.
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
                // Another concurrent request for the same device inserted first — fine, the
                // unique index on DeviceId means exactly one row exists either way.
                _db.ChangeTracker.Clear();
            }
        }

        // A single conditional UPDATE (guarded by the limit check in the WHERE clause) so two
        // concurrent requests for the same device can't both read Count=19, both pass an
        // in-memory check, and both consume — running the device past its allowance.
        var rows = await _db.AnonymousAiUsages
            .Where(a => a.DeviceId == deviceId && a.Count < limit)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Count, a => a.Count + 1)
                .SetProperty(a => a.LastUsedAt, DateTime.UtcNow));
        return rows > 0;
    }

    public async Task<int> GetRemainingAsync(string deviceId, int limit)
    {
        var count = await _db.AnonymousAiUsages
            .Where(a => a.DeviceId == deviceId)
            .Select(a => a.Count)
            .FirstOrDefaultAsync();
        return Math.Max(0, limit - count);
    }
}
