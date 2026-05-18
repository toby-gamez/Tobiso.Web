using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IPostService
{
    /// <summary>Returns all posts. When gradeId is supplied, Content/LastFix/LastEdit come from the best-matching version.</summary>
    Task<List<PostResponse>> GetAll(int? gradeId = null);
    Task<List<PostSummaryResponse>> GetSummaries();
    Task<List<PostLinkResponse>> GetLinks();
    /// <summary>Returns a single post including all its versions. When gradeId is supplied the top-level Content fields reflect the best match.</summary>
    Task<PostResponse?> GetById(int id, int? gradeId = null);
    /// <summary>Updates post metadata only (title, filepath, category). Version content is managed via IPostVersionService.</summary>
    Task<bool> UpdateMetadata(int id, UpdatePostRequest req);
    Task<bool> Delete(int id);
    Task<PostResponse?> Create(CreatePostRequest req);
}

public class PostService : IPostService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<PostService> _logger;

    public PostService(TobisoDbContext context, ILogger<PostService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static PostVersionResponse ToVersionResponse(PostVersion v) => new()
    {
        Id = v.Id,
        PostId = v.PostId,
        GradeId = v.GradeId,
        GradeName = v.Grade?.Name,
        GradeLevel = v.Grade?.Level,
        Content = v.Content,
        LastFix = v.LastFix,
        LastEdit = v.LastEdit
    };

    /// <summary>
    /// Picks the best version for the given preferred grade level:
    /// highest Grade.Level that is ≤ preferredLevel, falling back to the highest available.
    /// </summary>
    private static PostVersion? BestMatch(IEnumerable<PostVersion> versions, int preferredLevel)
    {
        var candidates = versions
            .Where(v => v.Grade != null && v.Grade.Level <= preferredLevel)
            .OrderByDescending(v => v.Grade!.Level)
            .FirstOrDefault();

        return candidates
            ?? versions.OrderByDescending(v => v.Grade?.Level ?? int.MinValue).FirstOrDefault();
    }

    /// <summary>
    /// Builds a PostResponse. <paramref name="versionsToInclude"/> determines what goes into Versions[]:
    /// pass a single best-match version when gradeId was specified; pass all versions otherwise.
    /// </summary>
    private static PostResponse BuildResponse(Post p, IEnumerable<PostVersion> versionsToInclude)
    {
        return new PostResponse
        {
            Id = p.Id,
            Title = p.Title,
            FilePath = p.FilePath,
            CategoryId = p.CategoryId,
            Versions = versionsToInclude.Select(ToVersionResponse).ToList()
        };
    }

    // ── IPostService ─────────────────────────────────────────────────────────

    public async Task<List<PostResponse>> GetAll(int? gradeId = null)
    {
        try
        {
            var posts = await _context.Posts
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Grade)
                .ToListAsync();

            int? preferredLevel = null;
            if (gradeId.HasValue)
            {
                var grade = await _context.Grades.FindAsync(gradeId.Value);
                if (grade == null) return new List<PostResponse>();
                preferredLevel = grade.Level;
            }

            var result = new List<PostResponse>();
            foreach (var p in posts)
            {
                if (preferredLevel.HasValue)
                {
                    var matched = BestMatch(p.Versions, preferredLevel.Value);
                    // Skip posts with no versions when a grade filter is active
                    if (matched == null) continue;
                    result.Add(BuildResponse(p, new[] { matched }));
                }
                else
                {
                    result.Add(BuildResponse(p, p.Versions));
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading posts");
            throw;
        }
    }

    public async Task<List<PostSummaryResponse>> GetSummaries()
    {
        return await _context.Posts
            .Select(p => new PostSummaryResponse
            {
                Id = p.Id,
                Title = p.Title,
                CategoryId = p.CategoryId,
                FilePath = p.FilePath,
                // Aggregate most recent timestamps across all versions for "last updated" display
                LastEdit = p.Versions.Max(v => (DateTime?)v.LastEdit),
                LastFix  = p.Versions.Max(v => (DateTime?)v.LastFix),
                AvailableGradeNames = p.Versions
                    .Where(v => v.Grade != null)
                    .OrderBy(v => v.Grade!.Level)
                    .Select(v => v.Grade!.Name)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<PostLinkResponse>> GetLinks()
    {
        return await _context.Posts
            .Select(p => new PostLinkResponse
            {
                Id = p.Id,
                Title = p.Title,
                FilePath = p.FilePath
            })
            .ToListAsync();
    }

    public async Task<PostResponse?> GetById(int id, int? gradeId = null)
    {
        var post = await _context.Posts
            .Include(p => p.Versions)
                .ThenInclude(v => v.Grade)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return null;

        if (gradeId.HasValue)
        {
            var grade = await _context.Grades.FindAsync(gradeId.Value);
            if (grade != null)
            {
                var matched = BestMatch(post.Versions, grade.Level);
                // Return only the matched version in Versions[]
                return BuildResponse(post, matched != null ? new[] { matched } : Array.Empty<PostVersion>());
            }
        }

        // No gradeId: return all versions
        return BuildResponse(post, post.Versions);
    }

    public async Task<bool> UpdateMetadata(int id, UpdatePostRequest req)
    {
        var entity = await _context.Posts.FindAsync(id);
        if (entity == null) return false;

        entity.Title = req.Title;
        entity.FilePath = req.FilePath;
        entity.CategoryId = req.CategoryId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            // Remove any RelatedPost entries that reference this post as RelatedPostId
            var relatedRefs = await _context.RelatedPosts
                .Where(r => r.RelatedPostId == id)
                .ToListAsync();
            if (relatedRefs.Any())
                _context.RelatedPosts.RemoveRange(relatedRefs);

            var entity = await _context.Posts
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return false;

            _context.Posts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            throw;
        }
    }

    public async Task<PostResponse?> Create(CreatePostRequest req)
    {
        var entity = new Post
        {
            Title = req.Title,
            FilePath = req.FilePath,
            CategoryId = req.CategoryId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(entity);
        await _context.SaveChangesAsync();

        PostVersion? version = null;
        if (req.GradeId.HasValue)
        {
            var now = DateTime.UtcNow;
            version = new PostVersion
            {
                PostId = entity.Id,
                GradeId = req.GradeId.Value,
                Content = req.Content,
                LastFix = req.IsFix ? now : null,
                LastEdit = req.IsFix ? null : now
            };
            _context.PostVersions.Add(version);
            await _context.SaveChangesAsync();
        }

        // Return the full response (reload with grade navigation)
        return await GetById(entity.Id);
    }
}
