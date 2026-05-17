using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

    public interface IPostService
    {
        // optional gradeId: prefer this grade or lower; when null return all posts latest version (highest level)
        Task<List<PostResponse>> GetAll(int? gradeId = null);
        Task<List<PostSummaryResponse>> GetSummaries();
        Task<List<PostLinkResponse>> GetLinks();
    	Task<PostResponse?> GetById(int id, int? gradeId = null);
        Task<bool> Update(PostResponse post);
        Task<bool> Delete(int id);
        Task<PostResponse?> Create(PostResponse post);
    };
public class PostService : IPostService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<PostService> _logger;

    public PostService(TobisoDbContext context, ILogger<PostService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PostResponse>> GetAll(int? gradeId = null)
    {
        try
        {
            // If gradeId is provided, compute best-match PostVersion per Post.
            if (gradeId.HasValue)
            {
                // resolve level
                var preferredGrade = await _context.Grades.FirstOrDefaultAsync(g => g.Id == gradeId.Value);
                if (preferredGrade == null) return new List<PostResponse>();

                var preferredLevel = preferredGrade.Level;

                // For each post, pick the version with Grade.Level <= preferredLevel, highest Level.
                var posts = await _context.Posts
                    .Include(p => p.Versions)!
                        .ThenInclude(v => v.Grade)
                    .ToListAsync();

                var result = new List<PostResponse>();
                foreach (var p in posts)
                {
                    var candidate = p.Versions
                        .Where(v => v.Grade != null && v.Grade.Level <= preferredLevel)
                        .OrderByDescending(v => v.Grade!.Level)
                        .FirstOrDefault();

                    // fallback: if no suitable version, pick highest-level version available
                    if (candidate == null)
                        candidate = p.Versions
                            .OrderByDescending(v => v.Grade?.Level ?? int.MinValue)
                            .FirstOrDefault();

                    if (candidate == null)
                    {
                        // no versions at all -> skip
                        continue;
                    }

                    result.Add(new PostResponse
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Content = candidate.Content,
                        FilePath = p.FilePath,
                        LastFix = candidate.LastFix,
                        LastEdit = candidate.LastEdit,
                        CategoryId = p.CategoryId,
                        GradeId = candidate.GradeId
                    });
                }

                return result;
            }

            // no grade filter: return latest version per post (highest grade level)
            var allPosts = await _context.Posts
                .Include(p => p.Versions)!
                    .ThenInclude(v => v.Grade)
                .ToListAsync();

            return allPosts.Select(p =>
            {
                var candidate = p.Versions
                    .OrderByDescending(v => v.Grade?.Level ?? int.MinValue)
                    .FirstOrDefault();
                if (candidate != null)
                {
                    return new PostResponse
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Content = candidate.Content,
                        FilePath = p.FilePath,
                        LastFix = candidate.LastFix,
                        LastEdit = candidate.LastEdit,
                        CategoryId = p.CategoryId,
                        GradeId = candidate.GradeId
                    };
                }
                // fallback to empty content
                return new PostResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = string.Empty,
                    FilePath = p.FilePath,
                    LastFix = null,
                    LastEdit = null,
                    CategoryId = p.CategoryId
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání příspěvků");
            throw;
        }

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

    public async Task<List<PostSummaryResponse>> GetSummaries()
    {
        // Return summaries based on latest/highest-level version per post
        var posts = await _context.Posts
            .Include(p => p.Versions)!
                .ThenInclude(v => v.Grade)
            .ToListAsync();

        return posts.Select(p =>
        {
            var candidate = p.Versions.OrderByDescending(v => v.Grade?.Level ?? int.MinValue).FirstOrDefault();
            if (candidate != null)
            {
                return new PostSummaryResponse
                {
                    Id = p.Id,
                    Title = p.Title,
                    CategoryId = p.CategoryId,
                    FilePath = p.FilePath,
                    LastFix = candidate.LastFix,
                    LastEdit = candidate.LastEdit
                };
            }
            return new PostSummaryResponse { Id = p.Id, Title = p.Title, CategoryId = p.CategoryId, FilePath = p.FilePath };
        }).ToList();
    }

    public async Task<PostResponse?> GetById(int id, int? gradeId = null)
    {
        var post = await _context.Posts
            .Include(p => p.Versions)!
                .ThenInclude(v => v.Grade)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return null;

        PostVersion? candidate = null;
        if (gradeId.HasValue)
        {
            var preferred = await _context.Grades.FirstOrDefaultAsync(g => g.Id == gradeId.Value);
            if (preferred != null)
            {
                candidate = post.Versions
                    .Where(v => v.Grade != null && v.Grade.Level <= preferred.Level)
                    .OrderByDescending(v => v.Grade!.Level)
                    .FirstOrDefault();
            }
        }

        candidate ??= post.Versions.OrderByDescending(v => v.Grade?.Level ?? int.MinValue).FirstOrDefault();

        if (candidate == null)
        {
            // fallback: no versions
            return new PostResponse
            {
                Id = post.Id,
                Title = post.Title,
                Content = string.Empty,
                FilePath = post.FilePath,
                LastFix = null,
                LastEdit = null,
                CategoryId = post.CategoryId
            };
        }

        return new PostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Content = candidate.Content,
            FilePath = post.FilePath,
            LastFix = candidate.LastFix,
            LastEdit = candidate.LastEdit,
            CategoryId = post.CategoryId,
            GradeId = candidate.GradeId
        };
    }

    public async Task<bool> Update(PostResponse post)
    {
        // Updating a post's metadata (title, filepath, category) remains on Post entity.
        var entity = await _context.Posts.FindAsync(post.Id);
        if (entity == null) return false;

        entity.Title = post.Title;
        entity.FilePath = post.FilePath;
        entity.CategoryId = post.CategoryId;

        // If GradeId is provided, update/create the corresponding PostVersion
        if (post.GradeId.HasValue)
        {
            var version = await _context.PostVersions.FirstOrDefaultAsync(v => v.PostId == post.Id && v.GradeId == post.GradeId.Value);
            if (version == null)
            {
                version = new PostVersion
                {
                    PostId = post.Id,
                    GradeId = post.GradeId,
                    Content = post.Content,
                    LastFix = post.LastFix,
                    LastEdit = post.LastEdit
                };
                _context.PostVersions.Add(version);
            }
            else
            {
                version.Content = post.Content;
                if (post.LastFix.HasValue) version.LastFix = post.LastFix;
                if (post.LastEdit.HasValue) version.LastEdit = post.LastEdit;
            }
        }
        else
        {
            // No GradeId: update the highest-level version if exists, else do nothing to content
            var highest = await _context.PostVersions.Where(v => v.PostId == post.Id).OrderByDescending(v => v.Grade!.Level).FirstOrDefaultAsync();
            if (highest != null)
            {
                highest.Content = post.Content;
                if (post.LastFix.HasValue) highest.LastFix = post.LastFix;
                if (post.LastEdit.HasValue) highest.LastEdit = post.LastEdit;
            }
        }

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
            {
                _context.RelatedPosts.RemoveRange(relatedRefs);
            }

            var entity = await _context.Posts
                .Include(p => p.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return false;

            _context.Posts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání příspěvku s Id={PostId}", id);
            throw;
        }
    }

    public async Task<PostResponse?> Create(PostResponse post)
    {
        var entity = new Tobiso.Web.Domain.Entities.Post
        {
            Title = post.Title,
            FilePath = post.FilePath,
            CategoryId = post.CategoryId
        };
        _context.Posts.Add(entity);
        await _context.SaveChangesAsync();

        // create version
        var version = new PostVersion
        {
            PostId = entity.Id,
            GradeId = post.GradeId,
            Content = post.Content,
            LastFix = post.LastFix ?? post.LastEdit ?? DateTime.UtcNow,
            LastEdit = post.LastEdit
        };
        _context.PostVersions.Add(version);
        await _context.SaveChangesAsync();

        var created = await _context.Posts.FirstOrDefaultAsync(p => p.Id == entity.Id);
        if (created == null) return null;
        return new PostResponse
        {
            Id = created.Id,
            Title = created.Title,
            Content = version.Content,
            FilePath = created.FilePath,
            LastFix = version.LastFix,
            LastEdit = version.LastEdit,
            CategoryId = created.CategoryId,
            GradeId = version.GradeId
        };
    }
}
