using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IUserDataExportService
{
    /// <summary>Builds a machine-readable JSON export of everything stored about the user, or null if the user doesn't exist.</summary>
    Task<byte[]?> ExportAsync(int userId);
}

public class UserDataExportService(TobisoDbContext db) : IUserDataExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Never includes the password hash. Covers every table keyed to the user (GDPR art. 15/20).
    public async Task<byte[]?> ExportAsync(int userId)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        var bookmarks = await db.UserBookmarks.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => new { b.PostId, PostTitle = b.Post.Title, b.CreatedAt })
            .ToListAsync();

        var notes = await db.UserNotes.AsNoTracking()
            .Where(n => n.UserId == userId)
            .Select(n => new { n.PostId, PostTitle = n.Post.Title, n.Content, n.CreatedAt, n.UpdatedAt })
            .ToListAsync();

        var readPosts = await db.UserReadPosts.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new { r.PostId, PostTitle = r.Post.Title, r.ScrollPercent, r.FirstReadAt, r.LastReadAt })
            .ToListAsync();

        var attempts = await db.QuestionAttempts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.QuestionId, a.LastCorrect, a.TimesCorrect, a.TimesWrong, a.FirstAttemptedAt, a.LastAttemptedAt })
            .ToListAsync();

        var credits = await db.AiCreditTransactions.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new { c.Delta, c.Reason, c.CreatedAt })
            .ToListAsync();

        var chats = await db.AiChatSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id, s.Title, s.PostId, s.CreatedAt, s.UpdatedAt,
                Messages = s.Messages.OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Role, m.Content, m.CreditsUsed, m.CreatedAt })
            })
            .ToListAsync();

        var export = new
        {
            ExportedAt = DateTime.UtcNow,
            Account = new
            {
                user.Email, user.DisplayName, user.AvatarUrl,
                SignInMethods = new { Password = !string.IsNullOrEmpty(user.PasswordHash), Google = user.GoogleId != null },
                user.Credits, user.CreatedAt, user.LastLoginAt, user.LastDailyBonusAt, user.LastReadBonusAt,
                user.TermsAcceptedAt, user.TermsVersion
            },
            Bookmarks = bookmarks,
            Notes = notes,
            ReadPosts = readPosts,
            QuestionAttempts = attempts,
            CreditTransactions = credits,
            AiChats = chats
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, JsonOptions);
    }
}
