using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IUserService
{
    Task<AppUser?> RegisterAsync(string email, string displayName, string password);
    Task<AppUser?> FindOrCreateGoogleUserAsync(string googleId, string email, string displayName, string? avatarUrl = null);
    Task<AppUser?> LoginAsync(string email, string password);
    Task<AppUser?> GetByIdAsync(int id);
    Task<bool> DeductCreditsAsync(int userId, int amount, string reason);
    Task<bool> ClaimDailyBonusAsync(int userId, int amount);
    Task<bool> ClaimReadBonusAsync(int userId, int amount);
}

public class UserService : IUserService
{
    private readonly TobisoDbContext _db;

    public UserService(TobisoDbContext db) => _db = db;

    public async Task<AppUser?> RegisterAsync(string email, string displayName, string password)
    {
        var normalizedEmail = email.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return null;

        var user = new AppUser
        {
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            PasswordHash = PasswordHasher.Hash(password),
            Credits = 20
        };
        _db.Users.Add(user);
        _db.AiCreditTransactions.Add(new AiCreditTransaction
        {
            User = user, Delta = 20, Reason = "registration_bonus"
        });
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser?> FindOrCreateGoogleUserAsync(string googleId, string email, string displayName, string? avatarUrl = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            if (avatarUrl != null) user.AvatarUrl = avatarUrl;
            await _db.SaveChangesAsync();
            return user;
        }

        var normalizedEmail = email.ToLowerInvariant();
        user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user != null)
        {
            user.GoogleId = googleId;
            user.LastLoginAt = DateTime.UtcNow;
            if (avatarUrl != null) user.AvatarUrl = avatarUrl;
            await _db.SaveChangesAsync();
            return user;
        }

        user = new AppUser
        {
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            GoogleId = googleId,
            AvatarUrl = avatarUrl,
            Credits = 20
        };
        _db.Users.Add(user);
        _db.AiCreditTransactions.Add(new AiCreditTransaction
        {
            User = user, Delta = 20, Reason = "registration_bonus"
        });
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return null;
        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return user;
    }

    // AsNoTracking: TobisoDbContext is scoped per-circuit in Blazor Server (and per-request
    // elsewhere), so a tracked read here can get cached in the identity map and then never
    // reflect later ExecuteUpdateAsync writes (DeductCreditsAsync, ClaimDailyBonusAsync, ...)
    // made against the same context instance, since those bypass the change tracker entirely.
    public Task<AppUser?> GetByIdAsync(int id) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    // Deduct is a single conditional UPDATE (guarded by the balance check in the WHERE
    // clause) rather than read-then-write, so two concurrent requests can't both read a
    // Credits=1 balance, both pass an in-memory check, and both deduct - overspending
    // beyond what the account actually has.
    public async Task<bool> DeductCreditsAsync(int userId, int amount, string reason)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var rows = await _db.Users
            .Where(u => u.Id == userId && u.Credits >= amount)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Credits, u => u.Credits - amount));
        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = -amount, Reason = reason });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    // Same conditional-UPDATE approach: the "already claimed today" check and the credit
    // grant happen in one atomic statement, guarded by LastDailyBonusAt in the WHERE clause,
    // so it can't be claimed twice via two concurrent requests, and - unlike checking
    // LastLoginAt - claiming always advances the stamp, so the endpoint can't be replayed
    // all day on a single long-lived JWT without ever logging in again.
    public async Task<bool> ClaimDailyBonusAsync(int userId, int amount)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var today = DateTime.UtcNow.Date;
        var rows = await _db.Users
            .Where(u => u.Id == userId && (u.LastDailyBonusAt == null || u.LastDailyBonusAt < today))
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Credits, u => u.Credits + amount)
                .SetProperty(u => u.LastDailyBonusAt, DateTime.UtcNow));
        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = amount, Reason = "daily_bonus" });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    // Reading an article extends the daily streak; award once per calendar day regardless of
    // how many articles or scroll-progress updates happen afterward - same atomic-UPDATE
    // idempotency pattern as ClaimDailyBonusAsync, tracked on a separate stamp so the two
    // bonuses don't interfere with each other.
    public async Task<bool> ClaimReadBonusAsync(int userId, int amount)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var today = DateTime.UtcNow.Date;
        var rows = await _db.Users
            .Where(u => u.Id == userId && (u.LastReadBonusAt == null || u.LastReadBonusAt < today))
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Credits, u => u.Credits + amount)
                .SetProperty(u => u.LastReadBonusAt, DateTime.UtcNow));
        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = amount, Reason = "read_streak_bonus" });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }
}
