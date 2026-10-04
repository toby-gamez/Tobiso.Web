using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleCategoryService
{
    Task<List<ChronicleCategoryResponse>> GetAll();
    Task<List<ChronicleCategoryTreeResponse>> GetTree();
    Task<List<int>> GetDescendantIds(int categoryId);
    Task<ChronicleCategoryResponse> Create(CreateChronicleCategoryRequest request);
    Task<ChronicleCategoryResponse> Update(int id, CreateChronicleCategoryRequest request);
    Task Delete(int id);
}

public class ChronicleCategoryService : IChronicleCategoryService
{
    private readonly TobisoDbContext _context;

    public ChronicleCategoryService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ChronicleCategoryResponse>> GetAll()
    {
        return await _context.ChronicleCategories
            .Select(c => new ChronicleCategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                Color = c.Color,
                Summary = c.Summary,
                Content = c.Content,
                ItemCount = c.Items.Count
            })
            .ToListAsync();
    }

    public async Task<List<ChronicleCategoryTreeResponse>> GetTree()
    {
        var categories = await _context.ChronicleCategories
            .Select(c => new { c.Id, c.Name, c.ParentId, c.Color })
            .ToListAsync();
        var lookup = categories.ToLookup(c => c.ParentId);

        List<ChronicleCategoryTreeResponse> BuildTree(int? parentId) =>
            lookup[parentId]
                .Select(c => new ChronicleCategoryTreeResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    Children = BuildTree(c.Id)
                }).ToList();

        return BuildTree(null);
    }

    public async Task<List<int>> GetDescendantIds(int categoryId)
    {
        var categories = await _context.ChronicleCategories.Select(c => new { c.Id, c.ParentId }).ToListAsync();
        var lookup = categories.ToLookup(c => c.ParentId);

        var result = new List<int> { categoryId };
        var queue = new Queue<int>();
        queue.Enqueue(categoryId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in lookup[current])
            {
                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }
        return result;
    }

    public async Task<ChronicleCategoryResponse> Create(CreateChronicleCategoryRequest request)
    {
        await ValidateParent(request.ParentId, existingId: null);

        var entity = new ChronicleCategory
        {
            Name = request.Name,
            ParentId = request.ParentId,
            Color = request.Color,
            Summary = request.Summary,
            Content = request.Content
        };
        _context.ChronicleCategories.Add(entity);
        await _context.SaveChangesAsync();

        return new ChronicleCategoryResponse
        {
            Id = entity.Id, Name = entity.Name, ParentId = entity.ParentId,
            Color = entity.Color, Summary = entity.Summary, Content = entity.Content
        };
    }

    public async Task<ChronicleCategoryResponse> Update(int id, CreateChronicleCategoryRequest request)
    {
        var entity = await _context.ChronicleCategories.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Kategorie kroniky nebyla nalezena.");

        await ValidateParent(request.ParentId, existingId: id);

        entity.Name = request.Name;
        entity.ParentId = request.ParentId;
        entity.Color = request.Color;
        entity.Summary = request.Summary;
        entity.Content = request.Content;
        await _context.SaveChangesAsync();

        return new ChronicleCategoryResponse
        {
            Id = entity.Id, Name = entity.Name, ParentId = entity.ParentId,
            Color = entity.Color, Summary = entity.Summary, Content = entity.Content
        };
    }

    public async Task Delete(int id)
    {
        var entity = await _context.ChronicleCategories.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Kategorie kroniky nebyla nalezena.");
        _context.ChronicleCategories.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private async Task ValidateParent(int? parentId, int? existingId)
    {
        if (!parentId.HasValue) return;
        if (parentId.Value == existingId)
            throw new InvalidOperationException("Kategorie nemůže být svým vlastním rodičem.");
        var parentExists = await _context.ChronicleCategories.AnyAsync(c => c.Id == parentId.Value);
        if (!parentExists)
            throw new InvalidOperationException($"Rodičovská kategorie s ID {parentId.Value} neexistuje.");
    }
}
