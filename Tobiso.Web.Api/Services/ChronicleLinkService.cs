using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleLinkService
{
    Task<List<ChronicleLinkResponse>> GetCauses(int itemId);
    Task<List<ChronicleLinkResponse>> GetConsequences(int itemId);
    Task<ChronicleLinkResponse> Create(CreateChronicleLinkRequest request);
    Task Delete(int id);
}

public class ChronicleLinkService : IChronicleLinkService
{
    private readonly TobisoDbContext _context;

    public ChronicleLinkService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<ChronicleLinkResponse> Projected() =>
        _context.ChronicleItemLinks.Select(l => new ChronicleLinkResponse
        {
            Id = l.Id,
            FromId = l.FromId,
            FromTitle = l.From!.Title,
            FromSlug = l.From.Slug,
            ToId = l.ToId,
            ToTitle = l.To!.Title,
            ToSlug = l.To.Slug,
            Type = l.Type,
            Explanation = l.Explanation
        });

    /// <summary>Links where this item is the consequence (i.e. its causes).</summary>
    public async Task<List<ChronicleLinkResponse>> GetCauses(int itemId) =>
        await Projected().Where(l => l.ToId == itemId).ToListAsync();

    /// <summary>Links where this item is the cause (i.e. its consequences).</summary>
    public async Task<List<ChronicleLinkResponse>> GetConsequences(int itemId) =>
        await Projected().Where(l => l.FromId == itemId).ToListAsync();

    public async Task<ChronicleLinkResponse> Create(CreateChronicleLinkRequest request)
    {
        if (request.FromId == request.ToId)
            throw new InvalidOperationException("Příčina a následek nemůžou být stejná položka.");

        var from = await _context.ChronicleItems.FindAsync(request.FromId);
        if (from == null) throw new InvalidOperationException("Zadaná příčina neexistuje.");
        var to = await _context.ChronicleItems.FindAsync(request.ToId);
        if (to == null) throw new InvalidOperationException("Zadaný následek neexistuje.");

        // Editor hlídá, že příčina nemůže být později než následek (doc §2).
        if (request.Type is ChronicleLinkType.Trigger or ChronicleLinkType.Consequence && from.StartYear > to.StartYear)
            throw new InvalidOperationException("Příčina nemůže nastat později než následek.");

        var entity = new ChronicleItemLink
        {
            FromId = request.FromId,
            ToId = request.ToId,
            Type = request.Type,
            Explanation = request.Explanation
        };
        _context.ChronicleItemLinks.Add(entity);
        await _context.SaveChangesAsync();

        return new ChronicleLinkResponse
        {
            Id = entity.Id, FromId = from.Id, FromTitle = from.Title, FromSlug = from.Slug,
            ToId = to.Id, ToTitle = to.Title, ToSlug = to.Slug, Type = entity.Type, Explanation = entity.Explanation
        };
    }

    public async Task Delete(int id)
    {
        var entity = await _context.ChronicleItemLinks.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Vazba kauzality nebyla nalezena.");
        _context.ChronicleItemLinks.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
