using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleRegionService
{
    Task<List<ChronicleRegionResponse>> GetAll();
    Task<ChronicleRegionResponse> Create(CreateChronicleRegionRequest request);
    Task Delete(int id);

    /// <summary>Resolves all region ids covered by the given regions, including descendants (via materialized Path prefix).</summary>
    Task<List<int>> ExpandToDescendants(IEnumerable<int> regionIds);
}

public class ChronicleRegionService : IChronicleRegionService
{
    private readonly TobisoDbContext _context;

    public ChronicleRegionService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ChronicleRegionResponse>> GetAll()
    {
        return await _context.ChronicleRegions
            .Select(r => new ChronicleRegionResponse { Id = r.Id, Name = r.Name, Type = r.Type, ParentId = r.ParentId, Path = r.Path })
            .ToListAsync();
    }

    public async Task<ChronicleRegionResponse> Create(CreateChronicleRegionRequest request)
    {
        string path;
        if (request.ParentId.HasValue)
        {
            var parent = await _context.ChronicleRegions.FindAsync(request.ParentId.Value);
            if (parent == null) throw new InvalidOperationException("Zadané nadřazené území neexistuje.");
            path = parent.Path; // finalized below once we know the new Id
        }
        else
        {
            path = "/";
        }

        var entity = new ChronicleRegion { Name = request.Name, Type = request.Type, ParentId = request.ParentId, Path = path };
        _context.ChronicleRegions.Add(entity);
        await _context.SaveChangesAsync();

        entity.Path = $"{path}{entity.Id}/";
        await _context.SaveChangesAsync();

        return new ChronicleRegionResponse { Id = entity.Id, Name = entity.Name, Type = entity.Type, ParentId = entity.ParentId, Path = entity.Path };
    }

    public async Task Delete(int id)
    {
        var entity = await _context.ChronicleRegions.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Území nebylo nalezeno.");
        _context.ChronicleRegions.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<int>> ExpandToDescendants(IEnumerable<int> regionIds)
    {
        var idList = regionIds.Distinct().ToList();
        if (idList.Count == 0) return new List<int>();

        var selected = await _context.ChronicleRegions.Where(r => idList.Contains(r.Id)).Select(r => r.Path).ToListAsync();
        if (selected.Count == 0) return idList;

        var all = await _context.ChronicleRegions.Select(r => new { r.Id, r.Path }).ToListAsync();
        var result = new HashSet<int>(idList);
        foreach (var region in all)
        {
            if (selected.Any(prefix => region.Path.StartsWith(prefix, StringComparison.Ordinal)))
                result.Add(region.Id);
        }
        return result.ToList();
    }
}
