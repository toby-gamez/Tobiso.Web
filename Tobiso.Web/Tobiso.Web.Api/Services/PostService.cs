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
        try
        {
            var posts = await _context.Posts.ToListAsync();
            return posts.Select(p => new PostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                FilePath = p.FilePath,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId
            }).ToList();
        }
        catch (Exception ex)
        {
            // Logování chyby - prozatím do konzole, případně použít logger
            Console.WriteLine($"Chyba při načítání příspěvků: {ex.Message}\n{ex.StackTrace}");
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
            UpdatedAt = post.UpdatedAt,
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
        entity.UpdatedAt = DateTime.UtcNow;
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
        var created = await _context.Posts.FirstOrDefaultAsync(p => p.Id == entity.Id);
        if (created == null) return null;
        return new PostResponse
        {
            Id = created.Id,
            Title = created.Title,
            Content = created.Content,
            FilePath = created.FilePath,
            UpdatedAt = created.UpdatedAt,
            CategoryId = created.CategoryId
        };
    }
}
