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
    Task AddCreditsAsync(int userId, int amount, string reason);
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

    public Task<AppUser?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> DeductCreditsAsync(int userId, int amount, string reason)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Credits < amount) return false;
        user.Credits -= amount;
        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = -amount, Reason = reason });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AddCreditsAsync(int userId, int amount, string reason)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return;
        user.Credits += amount;
        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = amount, Reason = reason });
        await _db.SaveChangesAsync();
    }
}
