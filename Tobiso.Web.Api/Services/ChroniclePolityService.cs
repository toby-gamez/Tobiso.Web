using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChroniclePolityService
{
    Task<List<ChroniclePolityResponse>> GetAll();
    Task<ChroniclePolityResponse> Create(CreateChroniclePolityRequest request);
    Task<ChroniclePolityTerritoryResponse> AddTerritory(CreateChroniclePolityTerritoryRequest request);

    /// <summary>"V té době: Velká Morava (část dnešního území)" style labels for a region at a given year.</summary>
    Task<List<ChroniclePolityLabel>> GetLabels(int regionId, long year);
}

public class ChroniclePolityService : IChroniclePolityService
{
    private readonly TobisoDbContext _context;

    public ChroniclePolityService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ChroniclePolityResponse>> GetAll()
    {
        return await _context.ChroniclePolities
            .Select(p => new ChroniclePolityResponse { Id = p.Id, Name = p.Name, StartYear = p.StartYear, EndYear = p.EndYear })
            .ToListAsync();
    }

    public async Task<ChroniclePolityResponse> Create(CreateChroniclePolityRequest request)
    {
        if (request.EndYear < request.StartYear)
            throw new InvalidOperationException("Rok zániku nemůže být před rokem vzniku.");

        var entity = new ChroniclePolity { Name = request.Name, StartYear = request.StartYear, EndYear = request.EndYear };
        _context.ChroniclePolities.Add(entity);
        await _context.SaveChangesAsync();

        return new ChroniclePolityResponse { Id = entity.Id, Name = entity.Name, StartYear = entity.StartYear, EndYear = entity.EndYear };
    }

    public async Task<ChroniclePolityTerritoryResponse> AddTerritory(CreateChroniclePolityTerritoryRequest request)
    {
        var polity = await _context.ChroniclePolities.FindAsync(request.PolityId);
        if (polity == null) throw new InvalidOperationException("Zadaný státní útvar neexistuje.");
        var regionExists = await _context.ChronicleRegions.AnyAsync(r => r.Id == request.RegionId);
        if (!regionExists) throw new InvalidOperationException("Zadané území neexistuje.");

        var entity = new ChroniclePolityTerritory
        {
            PolityId = request.PolityId,
            RegionId = request.RegionId,
            Coverage = request.Coverage,
            FromYear = request.FromYear,
            ToYear = request.ToYear
        };
        _context.ChroniclePolityTerritories.Add(entity);
        await _context.SaveChangesAsync();

        return new ChroniclePolityTerritoryResponse
        {
            Id = entity.Id, PolityId = entity.PolityId, PolityName = polity.Name, RegionId = entity.RegionId,
            Coverage = entity.Coverage, FromYear = entity.FromYear, ToYear = entity.ToYear
        };
    }

    public async Task<List<ChroniclePolityLabel>> GetLabels(int regionId, long year)
    {
        var territories = await _context.ChroniclePolityTerritories
            .Include(t => t.Polity)
            .Where(t => t.RegionId == regionId)
            .ToListAsync();

        return territories
            .Where(t => t.Polity != null
                        && year >= (t.FromYear ?? t.Polity.StartYear)
                        && year <= (t.ToYear ?? t.Polity.EndYear))
            .Select(t => new ChroniclePolityLabel { PolityName = t.Polity!.Name, Coverage = t.Coverage })
            .ToList();
    }
}
