using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.Helpers;

namespace Tobiso.Web.Api.Services;

public interface ILegalNoticeService
{
    /// <summary>
    /// Returns the date of the latest material change (LastEdit, not LastFix) to the Terms of Use or
    /// Privacy Policy that this existing user has not yet seen or been asked about; null when nothing is pending.
    /// </summary>
    Task<DateTime?> GetPendingChangeAsync(int userId);

    /// <summary>Remembers that the user closed the notice for the given change date.</summary>
    Task DismissAsync(int userId, DateTime changeDate);
}

public class LegalNoticeService : ILegalNoticeService
{
    private static readonly string[] LegalSlugs = { "terms-of-use-cs", "privacy-policy-cs" };

    private readonly TobisoDbContext _db;

    public LegalNoticeService(TobisoDbContext db) => _db = db;

    public async Task<DateTime?> GetPendingChangeAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.CreatedAt, u.TermsAcceptedAt, u.LegalNoticeDismissedFor })
            .FirstOrDefaultAsync();
        if (user == null) return null;

        var latest = await GetLatestChangeAsync();
        if (latest == null) return null;

        // Whoever agreed (or signed up) after the change already saw the current text.
        var seenUpTo = user.TermsAcceptedAt ?? user.CreatedAt;
        if (latest <= seenUpTo) return null;
        if (user.LegalNoticeDismissedFor != null && latest <= user.LegalNoticeDismissedFor) return null;

        return latest;
    }

    public async Task DismissAsync(int userId, DateTime changeDate)
    {
        await _db.Users.Where(u => u.Id == userId && (u.LegalNoticeDismissedFor == null || u.LegalNoticeDismissedFor < changeDate))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LegalNoticeDismissedFor, changeDate));
    }

    private async Task<DateTime?> GetLatestChangeAsync()
    {
        // No slug column - derive it from FilePath in memory, as PostService.GetBySlug does.
        var posts = await _db.Posts.AsNoTracking().Select(p => new { p.Id, p.FilePath }).ToListAsync();
        var ids = posts
            .Where(p => LegalSlugs.Contains(PostSlug.FromFilePath(p.FilePath), StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Id)
            .ToList();
        if (ids.Count == 0) return null;

        return await _db.PostVersions.AsNoTracking()
            .Where(v => ids.Contains(v.PostId))
            .MaxAsync(v => v.LastEdit);
    }
}
