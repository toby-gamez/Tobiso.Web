using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChroniclePeriodizationService
{
    Task<List<ChroniclePeriodizationResponse>> GetAll();
    Task<ChroniclePeriodizationResponse?> GetById(int id);
    Task<ChroniclePeriodizationResponse> Create(CreateChroniclePeriodizationRequest request);
    Task<ChroniclePeriodResponse> AddPeriod(CreateChroniclePeriodRequest request);
    Task DeletePeriod(int id);
    Task Delete(int id);
}

public class ChroniclePeriodizationService : IChroniclePeriodizationService
{
    private readonly TobisoDbContext _context;

    public ChroniclePeriodizationService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ChroniclePeriodizationResponse>> GetAll()
    {
        var periodizations = await _context.ChroniclePeriodizations
            .Include(p => p.Periods).ThenInclude(p => p.StartEvent)
            .ToListAsync();

        return periodizations.Select(ToResponse).ToList();
    }

    public async Task<ChroniclePeriodizationResponse?> GetById(int id)
    {
        var p = await _context.ChroniclePeriodizations
            .Include(x => x.Periods).ThenInclude(x => x.StartEvent)
            .FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? null : ToResponse(p);
    }

    public async Task<ChroniclePeriodizationResponse> Create(CreateChroniclePeriodizationRequest request)
    {
        if (request.IsDefault)
        {
            var currentDefaults = await _context.ChroniclePeriodizations.Where(p => p.IsDefault).ToListAsync();
            foreach (var d in currentDefaults) d.IsDefault = false;
        }

        var entity = new ChroniclePeriodization { Name = request.Name, IsDefault = request.IsDefault };
        _context.ChroniclePeriodizations.Add(entity);
        await _context.SaveChangesAsync();

        return new ChroniclePeriodizationResponse { Id = entity.Id, Name = entity.Name, IsDefault = entity.IsDefault };
    }

    public async Task<ChroniclePeriodResponse> AddPeriod(CreateChroniclePeriodRequest request)
    {
        var periodizationExists = await _context.ChroniclePeriodizations.AnyAsync(p => p.Id == request.PeriodizationId);
        if (!periodizationExists) throw new InvalidOperationException("Zadaná periodizace neexistuje.");

        ChronicleItem? startEvent = null;
        if (request.StartEventId.HasValue)
        {
            startEvent = await _context.ChronicleItems.FindAsync(request.StartEventId.Value);
            if (startEvent == null) throw new InvalidOperationException("Zadaná počáteční událost neexistuje.");
        }

        var entity = new ChroniclePeriod
        {
            PeriodizationId = request.PeriodizationId,
            Order = request.Order,
            Name = request.Name,
            StartYear = request.StartYear,
            StartEventId = request.StartEventId,
            Color = request.Color,
            DefaultCollapsed = request.DefaultCollapsed
        };
        _context.ChroniclePeriods.Add(entity);
        await _context.SaveChangesAsync();

        return new ChroniclePeriodResponse
        {
            Id = entity.Id,
            PeriodizationId = entity.PeriodizationId,
            Order = entity.Order,
            Name = entity.Name,
            EffectiveStartYear = startEvent?.StartYear ?? entity.StartYear,
            StartEventId = entity.StartEventId,
            Color = entity.Color,
            DefaultCollapsed = entity.DefaultCollapsed
        };
    }

    public async Task DeletePeriod(int id)
    {
        var entity = await _context.ChroniclePeriods.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Období nebylo nalezeno.");
        _context.ChroniclePeriods.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var entity = await _context.ChroniclePeriodizations.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Periodizace nebyla nalezena.");
        _context.ChroniclePeriodizations.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private static ChroniclePeriodizationResponse ToResponse(ChroniclePeriodization p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        IsDefault = p.IsDefault,
        Periods = p.Periods.OrderBy(x => x.Order).Select(x => new ChroniclePeriodResponse
        {
            Id = x.Id,
            PeriodizationId = x.PeriodizationId,
            Order = x.Order,
            Name = x.Name,
            EffectiveStartYear = x.StartEvent?.StartYear ?? x.StartYear,
            StartEventId = x.StartEventId,
            Color = x.Color,
            DefaultCollapsed = x.DefaultCollapsed
        }).ToList()
    };
}
