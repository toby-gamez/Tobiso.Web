using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IRelatedPostService
{
    Task<List<RelatedPostResponse>> GetAll();
    Task<List<RelatedPostResponse>> GetByPostId(int postId);
    Task<RelatedPostResponse?> GetById(int id);
    Task<RelatedPostResponse?> Create(CreateRelatedPostRequest request);
    Task<bool> Update(int id, UpdateRelatedPostRequest request);
    Task<bool> Delete(int id);
}

public class RelatedPostService : IRelatedPostService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<RelatedPostService> _logger;

    public RelatedPostService(TobisoDbContext context, ILogger<RelatedPostService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<RelatedPostResponse>> GetAll()
    {
        try
        {
            return await _context.RelatedPosts
                .AsNoTracking()
                .Select(rp => new RelatedPostResponse
                {
                    Id = rp.Id,
                    PostId = rp.PostId,
                    RelatedPostId = rp.RelatedPostId,
                    Text = rp.Text,
                    PostTitle = rp.Post != null ? rp.Post.Title : null,
                    RelatedPostTitle = rp.RelatedPostRef != null ? rp.RelatedPostRef.Title : null
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání souvisejících postů");
            throw;
        }
    }

    public async Task<List<RelatedPostResponse>> GetByPostId(int postId)
    {
        try
        {
            return await _context.RelatedPosts
                .AsNoTracking()
                .Where(rp => rp.PostId == postId)
                .Select(rp => new RelatedPostResponse
                {
                    Id = rp.Id,
                    PostId = rp.PostId,
                    RelatedPostId = rp.RelatedPostId,
                    Text = rp.Text,
                    PostTitle = rp.Post != null ? rp.Post.Title : null,
                    RelatedPostTitle = rp.RelatedPostRef != null ? rp.RelatedPostRef.Title : null
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání souvisejících postů pro post {PostId}", postId);
            throw;
        }
    }

    public async Task<RelatedPostResponse?> GetById(int id)
    {
        var relatedPost = await _context.RelatedPosts.AsNoTracking().FirstOrDefaultAsync(rp => rp.Id == id);
        if (relatedPost == null) return null;

        return new RelatedPostResponse
        {
            Id = relatedPost.Id,
            PostId = relatedPost.PostId,
            RelatedPostId = relatedPost.RelatedPostId,
            Text = relatedPost.Text,
        };
    }

    public async Task<RelatedPostResponse?> Create(CreateRelatedPostRequest request)
    {
        try
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == request.PostId);
            var relatedPostExists = await _context.Posts.AnyAsync(p => p.Id == request.RelatedPostId);

            if (!postExists || !relatedPostExists)
                return null;

            if (request.PostId == request.RelatedPostId)
                return null;

            var existingConnection = await _context.RelatedPosts
                .AnyAsync(rp => rp.PostId == request.PostId && rp.RelatedPostId == request.RelatedPostId);

            if (existingConnection)
                return null;

            var entity = new RelatedPost
            {
                PostId = request.PostId,
                RelatedPostId = request.RelatedPostId,
                Text = request.Text
            };

            _context.RelatedPosts.Add(entity);

            if (request.CreateReverse)
            {
                _context.RelatedPosts.Add(new RelatedPost
                {
                    PostId = request.RelatedPostId,
                    RelatedPostId = request.PostId,
                    Text = request.Text
                });
            }

            await _context.SaveChangesAsync();

            return await GetById(entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření souvisejícího postu");
            throw;
        }
    }

    public async Task<bool> Update(int id, UpdateRelatedPostRequest request)
    {
        try
        {
            var entity = await _context.RelatedPosts.FindAsync(id);
            if (entity == null) return false;

            var postExists = await _context.Posts.AnyAsync(p => p.Id == request.PostId);
            var relatedPostExists = await _context.Posts.AnyAsync(p => p.Id == request.RelatedPostId);

            if (!postExists || !relatedPostExists)
                return false;

            if (request.PostId == request.RelatedPostId)
                return false;

            entity.PostId = request.PostId;
            entity.RelatedPostId = request.RelatedPostId;
            entity.Text = request.Text;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci souvisejícího postu {Id}", id);
            throw;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var entity = await _context.RelatedPosts.FindAsync(id);
            if (entity == null) return false;

            _context.RelatedPosts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání souvisejícího postu {Id}", id);
            throw;
        }
    }
}
