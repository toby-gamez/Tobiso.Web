using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Tobiso.Web.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAll();
    Task<List<CategoryTreeResponse>> GetTree();
    Task<CategoryResponse> Create(CategoryResponse category);
    Task<CategoryResponse> Update(int id, CategoryResponse category);
    Task Delete(int id);
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

    public async Task<CategoryResponse> Create(CategoryResponse category)
    {
        if (category.ParentId.HasValue)
        {
            if (category.ParentId.Value == category.Id)
                throw new InvalidOperationException("Kategorie nemůže být svým vlastním rodičem.");
            var parentExists = await _context.Categories.AnyAsync(c => c.Id == category.ParentId.Value);
            if (!parentExists)
                throw new InvalidOperationException($"Rodičovská kategorie s ID {category.ParentId.Value} neexistuje.");
        }
        var entity = new Category
        {
            Name = category.Name,
            ParentId = category.ParentId
        };
        _context.Categories.Add(entity);
        await _context.SaveChangesAsync();
        return new CategoryResponse { Id = entity.Id, Name = entity.Name, ParentId = entity.ParentId };
    }

    public async Task<CategoryResponse> Update(int id, CategoryResponse category)
    {
        var entity = await _context.Categories.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Category not found");
        if (category.ParentId.HasValue)
        {
            if (category.ParentId.Value == id)
                throw new InvalidOperationException("Kategorie nemůže být svým vlastním rodičem.");
            var parentExists = await _context.Categories.AnyAsync(c => c.Id == category.ParentId.Value);
            if (!parentExists)
                throw new InvalidOperationException($"Rodičovská kategorie s ID {category.ParentId.Value} neexistuje.");
        }
        entity.Name = category.Name;
        entity.ParentId = category.ParentId;
        await _context.SaveChangesAsync();
        return new CategoryResponse { Id = entity.Id, Name = entity.Name, ParentId = entity.ParentId };
    }

    public async Task Delete(int id)
    {
        var entity = await _context.Categories.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Category not found");
        _context.Categories.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
