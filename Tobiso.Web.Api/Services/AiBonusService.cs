using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAiBonusService
{
    /// <summary>Stores a redeemed signed grant. False when this signature was already redeemed (replay).</summary>
    Task<bool> TryRedeemAsync(string deviceKey, int count, DateTime validUntil, string signature);

    /// <summary>Sum of the device's bonus requests that haven't expired yet.</summary>
    Task<int> GetBonusTotalAsync(string deviceKey);
}

public class AiBonusService(IDbContextFactory<TobisoDbContext> dbFactory) : IAiBonusService
{
    public async Task<bool> TryRedeemAsync(string deviceKey, int count, DateTime validUntil, string signature)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature.ToLowerInvariant())));
        await using var db = dbFactory.CreateDbContext();
        if (await db.AiBonusGrants.AnyAsync(g => g.SignatureHash == hash)) return false;

        db.AiBonusGrants.Add(new AiBonusGrant { DeviceKey = deviceKey, Count = count, ValidUntil = validUntil, SignatureHash = hash });
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false; // a concurrent redemption of the same signature won the unique index
        }
    }

    public async Task<int> GetBonusTotalAsync(string deviceKey)
    {
        var now = DateTime.UtcNow;
        await using var db = dbFactory.CreateDbContext();
        return await db.AiBonusGrants
            .Where(g => g.DeviceKey == deviceKey && g.ValidUntil > now)
            .SumAsync(g => (int?)g.Count) ?? 0;
    }
}
