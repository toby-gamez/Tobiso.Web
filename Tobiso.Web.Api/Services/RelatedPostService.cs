using Microsoft.EntityFrameworkCore;
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

    public RelatedPostService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<RelatedPostResponse>> GetAll()
    {
        try
        {
            var relatedPosts = await _context.RelatedPosts
                .Include(rp => rp.Post)
                .Include(rp => rp.RelatedPostRef)
                .ToListAsync();

            return relatedPosts.Select(rp => new RelatedPostResponse
            {
                Id = rp.Id,
                PostId = rp.PostId,
                RelatedPostId = rp.RelatedPostId,
                Text = rp.Text,
                PostTitle = rp.Post?.Title,
                RelatedPostTitle = rp.RelatedPostRef?.Title
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání souvisejících postů: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<RelatedPostResponse>> GetByPostId(int postId)
    {
        try
        {
            var relatedPosts = await _context.RelatedPosts
                .Include(rp => rp.Post)
                .Include(rp => rp.RelatedPostRef)
                .Where(rp => rp.PostId == postId)
                .ToListAsync();

            return relatedPosts.Select(rp => new RelatedPostResponse
            {
                Id = rp.Id,
                PostId = rp.PostId,
                RelatedPostId = rp.RelatedPostId,
                Text = rp.Text,
                PostTitle = rp.Post?.Title,
                RelatedPostTitle = rp.RelatedPostRef?.Title
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání souvisejících postů pro post {postId}: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public async Task<RelatedPostResponse?> GetById(int id)
    {
        var relatedPost = await _context.RelatedPosts
            .Include(rp => rp.Post)
            .Include(rp => rp.RelatedPostRef)
            .FirstOrDefaultAsync(rp => rp.Id == id);

        if (relatedPost == null) return null;

        return new RelatedPostResponse
        {
            Id = relatedPost.Id,
            PostId = relatedPost.PostId,
            RelatedPostId = relatedPost.RelatedPostId,
            Text = relatedPost.Text,
            PostTitle = relatedPost.Post?.Title,
            RelatedPostTitle = relatedPost.RelatedPostRef?.Title
        };
    }

    public async Task<RelatedPostResponse?> Create(CreateRelatedPostRequest request)
    {
        try
        {
            // Ověř, že oba posty existují
            var postExists = await _context.Posts.AnyAsync(p => p.Id == request.PostId);
            var relatedPostExists = await _context.Posts.AnyAsync(p => p.Id == request.RelatedPostId);

            if (!postExists || !relatedPostExists)
                return null;

            // Ověř, že se post neodkazuje sám na sebe
            if (request.PostId == request.RelatedPostId)
                return null;

            // Ověř, že spojení už neexistuje v tomto směru
            var existingConnection = await _context.RelatedPosts
                .AnyAsync(rp => rp.PostId == request.PostId && rp.RelatedPostId == request.RelatedPostId);

            if (existingConnection)
                return null;

            // Vytvoř hlavní spojení
            var entity = new RelatedPost
            {
                PostId = request.PostId,
                RelatedPostId = request.RelatedPostId,
                Text = request.Text
            };

            _context.RelatedPosts.Add(entity);

            // Vytvoř opačné spojení pouze pokud je to požadováno
            if (request.CreateReverse)
            {
                var reverseEntity = new RelatedPost
                {
                    PostId = request.RelatedPostId,
                    RelatedPostId = request.PostId,
                    Text = request.Text // Stejný text pro obě směry
                };

                _context.RelatedPosts.Add(reverseEntity);
            }

            await _context.SaveChangesAsync();

            // Načti vytvořený záznam s navigačními vlastnostmi
            return await GetById(entity.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při vytváření souvisejícího postu: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public async Task<bool> Update(int id, UpdateRelatedPostRequest request)
    {
        try
        {
            var entity = await _context.RelatedPosts.FindAsync(id);
            if (entity == null) return false;

            // Ověř, že oba posty existují
            var postExists = await _context.Posts.AnyAsync(p => p.Id == request.PostId);
            var relatedPostExists = await _context.Posts.AnyAsync(p => p.Id == request.RelatedPostId);

            if (!postExists || !relatedPostExists)
                return false;

            // Ověř, že se post neodkazuje sám na sebe
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
            Console.WriteLine($"Chyba při aktualizaci souvisejícího postu {id}: {ex.Message}\n{ex.StackTrace}");
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
            Console.WriteLine($"Chyba při mazání souvisejícího postu {id}: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
}