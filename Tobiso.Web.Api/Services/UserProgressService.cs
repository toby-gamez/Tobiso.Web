using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IUserProgressService
{
    Task UpsertReadProgressAsync(int userId, int postId, int scrollPercent);
    Task<List<int>> GetBookmarkIdsAsync(int userId);
    Task AddBookmarkAsync(int userId, int postId);
    Task RemoveBookmarkAsync(int userId, int postId);
    Task<UserStatsDto> GetStatsAsync(int userId);
}

public class UserProgressService : IUserProgressService
{
    private readonly TobisoDbContext _db;

    public UserProgressService(TobisoDbContext db) => _db = db;

    public async Task UpsertReadProgressAsync(int userId, int postId, int scrollPercent)
    {
        var record = await _db.UserReadPosts
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PostId == postId);

        if (record == null)
        {
            _db.UserReadPosts.Add(new UserReadPost
            {
                UserId = userId,
                PostId = postId,
                ScrollPercent = scrollPercent,
                FirstReadAt = DateTime.UtcNow,
                LastReadAt = DateTime.UtcNow
            });
        }
        else
        {
            record.ScrollPercent = Math.Max(record.ScrollPercent, scrollPercent);
            record.LastReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetBookmarkIdsAsync(int userId) =>
        await _db.UserBookmarks
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => b.PostId)
            .ToListAsync();

    public async Task AddBookmarkAsync(int userId, int postId)
    {
        if (!await _db.UserBookmarks.AnyAsync(b => b.UserId == userId && b.PostId == postId))
        {
            _db.UserBookmarks.Add(new UserBookmark { UserId = userId, PostId = postId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveBookmarkAsync(int userId, int postId)
    {
        var bm = await _db.UserBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PostId == postId);
        if (bm != null)
        {
            _db.UserBookmarks.Remove(bm);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<UserStatsDto> GetStatsAsync(int userId)
    {
        var readPosts = await _db.UserReadPosts
            .Where(r => r.UserId == userId)
            .Include(r => r.Post)
                .ThenInclude(p => p.Category)
                    .ThenInclude(c => c!.Parent)
            .ToListAsync();

        var totalRead = readPosts.Select(r => r.PostId).Distinct().Count();

        // Streak: count consecutive days ending today
        var distinctDates = readPosts
            .Select(r => r.LastReadAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var streak = 0;
        var expected = DateTime.UtcNow.Date;
        foreach (var date in distinctDates)
        {
            if (date == expected || date == expected.AddDays(-1) && streak == 0)
            {
                streak++;
                expected = date.AddDays(-1);
            }
            else if (date < expected) break;
        }

        // Per-subject counts
        var perSubject = readPosts
            .Where(r => r.Post?.Category != null)
            .GroupBy(r =>
            {
                var cat = r.Post!.Category!;
                return cat.ParentId == null ? cat.Name : (cat.Parent?.Name ?? cat.Name);
            })
            .Select(g => new SubjectReadDto(g.Key, g.Select(r => r.PostId).Distinct().Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        // Badges based on article count per subject
        var badges = perSubject
            .Select(s => new { s.SubjectName, s.Count })
            .Where(s => s.Count >= 5)
            .Select(s => new BadgeDto(s.SubjectName, s.Count switch
            {
                >= 30 => "Expert",
                >= 15 => "Pokročilý",
                _ => "Začátečník"
            }))
            .ToList();

        return new UserStatsDto(streak, totalRead, perSubject, badges);
    }
}
