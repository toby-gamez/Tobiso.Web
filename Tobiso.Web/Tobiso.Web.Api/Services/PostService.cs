using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IPostService
{
    Task<List<PostResponse>> GetAll();
    Task<List<PostLinkResponse>> GetLinks();
    Task<PostResponse?> GetById(int id);
    Task<bool> Update(PostResponse post);
    Task<bool> Delete(int id);
    Task<PostResponse?> Create(PostResponse post);
};
public class PostService : IPostService
{
    private readonly TobisoDbContext _context;

    public PostService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<PostResponse>> GetAll()
    {
        return await _context.Posts
            .Include(p => p.Category)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                FilePath = p.FilePath,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId,
                Category = p.Category
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

    public async Task<PostResponse?> GetById(int id)
    {
        return await _context.Posts
            .Include(p => p.Category)
            .Where(p => p.Id == id)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                FilePath = p.FilePath,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId,
                Category = p.Category
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> Update(PostResponse post)
    {
        var entity = await _context.Posts.FindAsync(post.Id);
        if (entity == null) return false;

        entity.Title = post.Title;
        entity.Content = post.Content;
        entity.FilePath = post.FilePath;
        entity.UpdatedAt = post.UpdatedAt;
        entity.CategoryId = post.CategoryId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var entity = await _context.Posts.FindAsync(id);
        if (entity == null) return false;

        _context.Posts.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PostResponse?> Create(PostResponse post)
    {
        var entity = new Tobiso.Web.Domain.Entities.Post
        {
            Title = post.Title,
            Content = post.Content,
            FilePath = post.FilePath,
            UpdatedAt = post.UpdatedAt ?? DateTime.UtcNow,
            CategoryId = post.CategoryId
        };
        _context.Posts.Add(entity);
        await _context.SaveChangesAsync();
        // načtení včetně kategorie
        var created = await _context.Posts.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == entity.Id);
        if (created == null) return null;
        return new PostResponse
        {
            Id = created.Id,
            Title = created.Title,
            Content = created.Content,
            FilePath = created.FilePath,
            UpdatedAt = created.UpdatedAt,
            CategoryId = created.CategoryId,
            Category = created.Category
        };
    }
}
