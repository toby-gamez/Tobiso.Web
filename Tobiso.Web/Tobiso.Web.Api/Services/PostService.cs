using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IPostService
{
    Task<List<PostResponse>> GetAll();
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
                FilePath = p.FilePath,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId,
                Category = p.Category
            })
            .ToListAsync();

    }
}
