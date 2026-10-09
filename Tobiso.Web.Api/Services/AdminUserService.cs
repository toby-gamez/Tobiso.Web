using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public record AdminUserRow(
    int Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool HasPassword,
    bool HasGoogle,
    int Credits,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int ChatCount);

public record AdminUserPage(List<AdminUserRow> Items, int TotalCount);

public interface IAdminUserService
{
    Task<AdminUserPage> GetPagedAsync(string? search, int page, int pageSize);
    Task<bool> AdjustCreditsAsync(int userId, int delta, string? reason);
    Task<bool> SetPasswordAsync(int userId, string newPassword);
    Task<bool> DeleteAsync(int userId);
}

public class AdminUserService : IAdminUserService
{
    private readonly TobisoDbContext _db;

    public AdminUserService(TobisoDbContext db) => _db = db;

    public async Task<AdminUserPage> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Email.Contains(term) || u.DisplayName.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserRow(
                u.Id, u.Email, u.DisplayName, u.AvatarUrl,
                u.PasswordHash != null, u.GoogleId != null,
                u.Credits, u.CreatedAt, u.LastLoginAt, u.ChatSessions.Count))
            .ToListAsync();

        return new AdminUserPage(items, total);
    }

    // Conditional UPDATE keeps the balance from going negative under concurrent spends,
    // same approach as UserService.DeductCreditsAsync.
    public async Task<bool> AdjustCreditsAsync(int userId, int delta, string? reason)
    {
        if (delta == 0) return false;

        await using var tx = await _db.Database.BeginTransactionAsync();
        var rows = await _db.Users
            .Where(u => u.Id == userId && u.Credits + delta >= 0)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Credits, u => u.Credits + delta));
        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        _db.AiCreditTransactions.Add(new AiCreditTransaction
        {
            UserId = userId,
            Delta = delta,
            Reason = string.IsNullOrWhiteSpace(reason) ? "admin_adjustment" : $"admin: {reason.Trim()}"
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> SetPasswordAsync(int userId, string newPassword)
    {
        var hash = PasswordHasher.Hash(newPassword);
        var rows = await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, hash));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int userId)
    {
        var rows = await _db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
        return rows > 0;
    }
}
