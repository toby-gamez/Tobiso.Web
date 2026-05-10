using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IPostService
{
    Task<List<PostResponse>> GetAll();
    Task<List<PostSummaryResponse>> GetSummaries();
    Task<List<PostLinkResponse>> GetLinks();
    Task<PostResponse?> GetById(int id);
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

    public async Task<List<PostResponse>> GetAll()
    {
        try
        {
            var posts = await _context.Posts.ToListAsync();
            return posts.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                FilePath = p.FilePath,
                LastFix = p.LastFix,
                LastEdit = p.LastEdit,
                CategoryId = p.CategoryId
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
        return await _context.Posts
            .Select(p => new PostSummaryResponse
            {
                Id = p.Id,
                Title = p.Title,
                CategoryId = p.CategoryId,
                FilePath = p.FilePath,
                LastFix = p.LastFix,
                LastEdit = p.LastEdit
            })
            .ToListAsync();
    }

    public async Task<PostResponse?> GetById(int id)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return null;
        return new PostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            FilePath = post.FilePath,
            LastFix = post.LastFix,
            LastEdit = post.LastEdit,
            CategoryId = post.CategoryId
        };
    }

    public async Task<bool> Update(PostResponse post)
    {
        var entity = await _context.Posts.FindAsync(post.Id);
        if (entity == null) return false;

        entity.Title = post.Title;
        entity.Content = post.Content;
        entity.FilePath = post.FilePath;
        // Update LastFix / LastEdit only if provided in DTO. This allows caller (admin) to decide which timestamp to bump.
        if (post.LastFix.HasValue)
            entity.LastFix = post.LastFix;
        if (post.LastEdit.HasValue)
            entity.LastEdit = post.LastEdit;
        entity.CategoryId = post.CategoryId;

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
            Content = post.Content,
            FilePath = post.FilePath,
            LastFix = post.LastFix ?? post.LastEdit ?? DateTime.UtcNow,
            LastEdit = post.LastEdit,
            CategoryId = post.CategoryId
        };
        _context.Posts.Add(entity);
        await _context.SaveChangesAsync();
        // načtení včetně kategorie
        var created = await _context.Posts.FirstOrDefaultAsync(p => p.Id == entity.Id);
        if (created == null) return null;
        return new PostResponse
        {
            Id = created.Id,
            Title = created.Title,
            Content = created.Content,
            FilePath = created.FilePath,
            LastFix = created.LastFix,
            LastEdit = created.LastEdit,
            CategoryId = created.CategoryId
        };
    }
}
