using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Tobiso.Web.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAll();
    Task<List<CategoryTreeResponse>> GetTree();
}

public class CategoryService : ICategoryService
{
    private readonly TobisoDbContext _context;

    public CategoryService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<CategoryResponse>> GetAll()
    {
        return await _context.Categories
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId
            })
            .ToListAsync();
    }

    public async Task<List<CategoryTreeResponse>> GetTree()
    {
        var categories = await _context.Categories.ToListAsync();
        var lookup = categories.ToLookup(c => c.ParentId);
        List<CategoryTreeResponse> BuildTree(int? parentId)
        {
            return lookup[parentId]
                .Select(c => new CategoryTreeResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Children = BuildTree(c.Id)
                }).ToList();
        }
        return BuildTree(null);
    }
}
