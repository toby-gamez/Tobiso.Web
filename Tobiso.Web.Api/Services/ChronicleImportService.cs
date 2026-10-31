using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleImportService
{
    /// <summary>Bulk-creates/updates kronika data from a JSON-shaped payload inside one transaction (doc §7).</summary>
    Task<ChronicleImportResult> Import(ChronicleImportRequest request);
}

public class ChronicleImportService : IChronicleImportService
{
    private readonly TobisoDbContext _context;

    public ChronicleImportService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ChronicleImportResult> Import(ChronicleImportRequest request)
    {
        var result = new ChronicleImportResult();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var categoryByName = await ResolveCategories(request.Categories, result);
            var regionByName = await ResolveRegions(request.Regions, result);
            var itemBySlug = await ResolveItems(request.Items, categoryByName, regionByName, result);
            await ResolveLinks(request.Links, itemBySlug, result);
            await ResolveAxisEntries(request, itemBySlug, result);

            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Dictionary<string, ChronicleCategory>> ResolveCategories(
        List<ChronicleImportCategory> imports, ChronicleImportResult result)
    {
        var byName = (await _context.ChronicleCategories.ToListAsync())
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        var pending = new List<ChronicleImportCategory>(imports);
        bool progressed = true;
        while (pending.Count > 0 && progressed)
        {
            progressed = false;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var c = pending[i];
                if (c.ParentName != null && !byName.ContainsKey(c.ParentName)) continue;
                if (byName.ContainsKey(c.Name)) { pending.RemoveAt(i); progressed = true; continue; }

                var entity = new ChronicleCategory
                {
                    Name = c.Name,
                    ParentId = c.ParentName != null ? byName[c.ParentName].Id : null,
                    Color = c.Color,
                    Summary = c.Summary
                };
                _context.ChronicleCategories.Add(entity);
                await _context.SaveChangesAsync();
                byName[c.Name] = entity;
                result.CategoriesCreated++;
                pending.RemoveAt(i);
                progressed = true;
            }
        }
        foreach (var c in pending)
            result.Errors.Add($"Kategorie '{c.Name}': nepodařilo se vyřešit nadřazenou kategorii '{c.ParentName}'.");

        return byName;
    }

    private async Task<Dictionary<string, ChronicleRegion>> ResolveRegions(
        List<ChronicleImportRegion> imports, ChronicleImportResult result)
    {
        var byName = (await _context.ChronicleRegions.ToListAsync())
            .ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);

        var pending = new List<ChronicleImportRegion>(imports);
        bool progressed = true;
        while (pending.Count > 0 && progressed)
        {
            progressed = false;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var r = pending[i];
                if (r.ParentName != null && !byName.ContainsKey(r.ParentName)) continue;
                if (byName.ContainsKey(r.Name)) { pending.RemoveAt(i); progressed = true; continue; }

                var parent = r.ParentName != null ? byName[r.ParentName] : null;
                var entity = new ChronicleRegion { Name = r.Name, Type = r.Type, ParentId = parent?.Id, Path = parent?.Path ?? "/" };
                _context.ChronicleRegions.Add(entity);
                await _context.SaveChangesAsync();
                entity.Path = $"{entity.Path}{entity.Id}/";
                await _context.SaveChangesAsync();
                byName[r.Name] = entity;
                result.RegionsCreated++;
                pending.RemoveAt(i);
                progressed = true;
            }
        }
        foreach (var r in pending)
            result.Errors.Add($"Území '{r.Name}': nepodařilo se vyřešit nadřazené území '{r.ParentName}'.");

        return byName;
    }

    private async Task<Dictionary<string, ChronicleItem>> ResolveItems(
        List<ChronicleImportItem> imports,
        Dictionary<string, ChronicleCategory> categoryByName,
        Dictionary<string, ChronicleRegion> regionByName,
        ChronicleImportResult result)
    {
        var bySlug = (await _context.ChronicleItems.ToListAsync())
            .ToDictionary(i => i.Slug, i => i, StringComparer.OrdinalIgnoreCase);

        foreach (var itemImport in imports)
        {
            if (string.IsNullOrWhiteSpace(itemImport.Slug) || string.IsNullOrWhiteSpace(itemImport.Title))
            {
                result.Errors.Add("Položka bez slugu nebo názvu byla přeskočena.");
                continue;
            }

            var isNew = !bySlug.TryGetValue(itemImport.Slug, out var entity);
            if (isNew)
            {
                entity = itemImport.ItemType.Equals(ChronicleItemTypeConstants.Person, StringComparison.OrdinalIgnoreCase)
                    ? new ChroniclePerson()
                    : new ChronicleEvent();
                entity.Slug = itemImport.Slug;
            }

            entity!.Title = itemImport.Title;
            entity.Summary = itemImport.Summary;
            entity.Content = itemImport.Content;
            entity.StartYear = itemImport.StartYear;
            entity.EndYear = itemImport.EndYear;
            entity.Precision = itemImport.Precision;
            entity.Reliability = itemImport.Reliability;
            entity.ReliabilityNote = itemImport.ReliabilityNote;
            entity.Importance = itemImport.Importance;
            if (entity is ChroniclePerson person) person.Occupation = itemImport.Occupation;

            if (isNew)
            {
                _context.ChronicleItems.Add(entity);
                await _context.SaveChangesAsync();
                bySlug[entity.Slug] = entity;
                result.ItemsCreated++;
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
                result.ItemsUpdated++;
            }

            await SyncImportCategories(entity, itemImport, categoryByName, result);
            await SyncImportRegions(entity, itemImport, regionByName, result);
            await _context.SaveChangesAsync();
        }

        return bySlug;
    }

    private async Task SyncImportCategories(
        ChronicleItem entity, ChronicleImportItem itemImport,
        Dictionary<string, ChronicleCategory> categoryByName, ChronicleImportResult result)
    {
        var existing = await _context.ChronicleItemCategories.Where(c => c.ItemId == entity.Id).ToListAsync();
        _context.ChronicleItemCategories.RemoveRange(existing);

        foreach (var catName in itemImport.CategoryNames)
        {
            if (!categoryByName.TryGetValue(catName, out var cat))
            {
                result.Errors.Add($"Položka '{itemImport.Slug}': kategorie '{catName}' nenalezena.");
                continue;
            }
            _context.ChronicleItemCategories.Add(new ChronicleItemCategory
            {
                ItemId = entity.Id,
                CategoryId = cat.Id,
                IsPrimary = catName.Equals(itemImport.PrimaryCategoryName, StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    private async Task SyncImportRegions(
        ChronicleItem entity, ChronicleImportItem itemImport,
        Dictionary<string, ChronicleRegion> regionByName, ChronicleImportResult result)
    {
        var existing = await _context.ChronicleItemRegions.Where(r => r.ItemId == entity.Id).ToListAsync();
        _context.ChronicleItemRegions.RemoveRange(existing);

        foreach (var regionName in itemImport.RegionNames)
        {
            if (!regionByName.TryGetValue(regionName, out var region))
            {
                result.Errors.Add($"Položka '{itemImport.Slug}': území '{regionName}' nenalezeno.");
                continue;
            }
            _context.ChronicleItemRegions.Add(new ChronicleItemRegion { ItemId = entity.Id, RegionId = region.Id });
        }
    }

    private async Task ResolveLinks(
        List<ChronicleImportLink> imports, Dictionary<string, ChronicleItem> itemBySlug, ChronicleImportResult result)
    {
        foreach (var link in imports)
        {
            if (!itemBySlug.TryGetValue(link.FromSlug, out var from) || !itemBySlug.TryGetValue(link.ToSlug, out var to))
            {
                result.Errors.Add($"Vazba {link.FromSlug} → {link.ToSlug}: položka nenalezena.");
                continue;
            }
            if (from.Id == to.Id)
            {
                result.Errors.Add($"Vazba {link.FromSlug} → {link.ToSlug}: příčina a následek nemůžou být stejná položka.");
                continue;
            }
            if (link.Type is ChronicleLinkType.Trigger or ChronicleLinkType.Consequence && from.StartYear > to.StartYear)
            {
                result.Errors.Add($"Vazba {link.FromSlug} → {link.ToSlug}: příčina nemůže být později než následek.");
                continue;
            }

            var exists = await _context.ChronicleItemLinks
                .AnyAsync(l => l.FromId == from.Id && l.ToId == to.Id && l.Type == link.Type);
            if (exists) continue;

            _context.ChronicleItemLinks.Add(new ChronicleItemLink
            {
                FromId = from.Id, ToId = to.Id, Type = link.Type, Explanation = link.Explanation
            });
            result.LinksCreated++;
        }
        await _context.SaveChangesAsync();
    }

    private async Task ResolveAxisEntries(
        ChronicleImportRequest request, Dictionary<string, ChronicleItem> itemBySlug, ChronicleImportResult result)
    {
        if (string.IsNullOrWhiteSpace(request.TargetAxisSlug)) return;

        var axis = await _context.ChronicleAxes.FirstOrDefaultAsync(a => a.Slug == request.TargetAxisSlug);
        if (axis == null)
        {
            axis = new ChronicleAxis { Slug = request.TargetAxisSlug, Name = request.TargetAxisName ?? request.TargetAxisSlug };
            _context.ChronicleAxes.Add(axis);
            await _context.SaveChangesAsync();
        }

        var existingItemIds = await _context.ChronicleAxisEntries
            .Where(e => e.AxisId == axis.Id && e.ItemId != null)
            .Select(e => e.ItemId!.Value)
            .ToListAsync();

        foreach (var itemImport in request.Items)
        {
            if (!itemBySlug.TryGetValue(itemImport.Slug, out var item)) continue;
            if (existingItemIds.Contains(item.Id)) continue;

            _context.ChronicleAxisEntries.Add(new ChronicleAxisEntry { AxisId = axis.Id, ItemId = item.Id });
            result.AxisEntriesCreated++;
        }
        await _context.SaveChangesAsync();
    }
}
