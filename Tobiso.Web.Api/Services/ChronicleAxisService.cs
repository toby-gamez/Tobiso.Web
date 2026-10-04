using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleAxisService
{
    Task<List<ChronicleAxisResponse>> GetAll();
    Task<ChronicleAxisResponse?> GetBySlug(string slug);
    Task<ChronicleAxisResponse> Create(CreateChronicleAxisRequest request);
    Task<List<ChronicleAxisEntryResponse>> GetEntries(int axisId);
    Task<ChronicleAxisEntryResponse> AddEntry(CreateChronicleAxisEntryRequest request);
    Task RemoveEntry(int entryId);

    /// <summary>Resolves axis entries (expanding categories per ExpandChildren/filter) into a cached layout.</summary>
    Task<ChronicleLayoutResponse> GetLayout(string axisSlug, ChronicleFilter filter);
}

public class ChronicleAxisService : IChronicleAxisService
{
    private readonly TobisoDbContext _context;
    private readonly IChronicleCategoryService _categoryService;
    private readonly IChronicleRegionService _regionService;
    private readonly IChroniclePeriodizationService _periodizationService;
    private readonly IMemoryCache _cache;

    public ChronicleAxisService(
        TobisoDbContext context,
        IChronicleCategoryService categoryService,
        IChronicleRegionService regionService,
        IChroniclePeriodizationService periodizationService,
        IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _categoryService = categoryService;
        _regionService = regionService;
        _periodizationService = periodizationService;
        _cache = cache;
    }

    public async Task<List<ChronicleAxisResponse>> GetAll() =>
        await _context.ChronicleAxes
            .Select(a => new ChronicleAxisResponse { Id = a.Id, Name = a.Name, Slug = a.Slug })
            .ToListAsync();

    public async Task<ChronicleAxisResponse?> GetBySlug(string slug) =>
        await _context.ChronicleAxes.Where(a => a.Slug == slug)
            .Select(a => new ChronicleAxisResponse { Id = a.Id, Name = a.Name, Slug = a.Slug })
            .FirstOrDefaultAsync();

    public async Task<ChronicleAxisResponse> Create(CreateChronicleAxisRequest request)
    {
        var slugTaken = await _context.ChronicleAxes.AnyAsync(a => a.Slug == request.Slug);
        if (slugTaken) throw new InvalidOperationException($"Osa se slugem '{request.Slug}' už existuje.");

        var entity = new ChronicleAxis { Name = request.Name, Slug = request.Slug };
        _context.ChronicleAxes.Add(entity);
        await _context.SaveChangesAsync();
        return new ChronicleAxisResponse { Id = entity.Id, Name = entity.Name, Slug = entity.Slug };
    }

    public async Task<List<ChronicleAxisEntryResponse>> GetEntries(int axisId) =>
        await _context.ChronicleAxisEntries
            .Where(e => e.AxisId == axisId)
            .Select(e => new ChronicleAxisEntryResponse
            {
                Id = e.Id,
                AxisId = e.AxisId,
                ItemId = e.ItemId,
                ItemTitle = e.Item != null ? e.Item.Title : null,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : null,
                ExpandChildren = e.ExpandChildren,
                OrderOverride = e.OrderOverride
            })
            .ToListAsync();

    public async Task<ChronicleAxisEntryResponse> AddEntry(CreateChronicleAxisEntryRequest request)
    {
        if (request.ItemId.HasValue == request.CategoryId.HasValue)
            throw new InvalidOperationException("Záznam osy musí odkazovat buď na položku, nebo na kategorii.");

        var axisExists = await _context.ChronicleAxes.AnyAsync(a => a.Id == request.AxisId);
        if (!axisExists) throw new InvalidOperationException("Zadaná osa neexistuje.");

        var entity = new ChronicleAxisEntry
        {
            AxisId = request.AxisId,
            ItemId = request.ItemId,
            CategoryId = request.CategoryId,
            ExpandChildren = request.ExpandChildren,
            OrderOverride = request.OrderOverride
        };
        _context.ChronicleAxisEntries.Add(entity);
        await _context.SaveChangesAsync();

        var item = request.ItemId.HasValue ? await _context.ChronicleItems.FindAsync(request.ItemId.Value) : null;
        var category = request.CategoryId.HasValue ? await _context.ChronicleCategories.FindAsync(request.CategoryId.Value) : null;

        return new ChronicleAxisEntryResponse
        {
            Id = entity.Id,
            AxisId = entity.AxisId,
            ItemId = entity.ItemId,
            ItemTitle = item?.Title,
            CategoryId = entity.CategoryId,
            CategoryName = category?.Name,
            ExpandChildren = entity.ExpandChildren,
            OrderOverride = entity.OrderOverride
        };
    }

    public async Task RemoveEntry(int entryId)
    {
        var entity = await _context.ChronicleAxisEntries.FindAsync(entryId);
        if (entity == null) return;
        _context.ChronicleAxisEntries.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<ChronicleLayoutResponse> GetLayout(string axisSlug, ChronicleFilter filter)
    {
        var cacheKey = $"chronicle-layout:v{ChronicleCacheVersion.Current}:{axisSlug}:{filter.CacheKey()}";
        if (_cache.TryGetValue(cacheKey, out ChronicleLayoutResponse? cached) && cached != null)
            return cached;

        var axis = await _context.ChronicleAxes.FirstOrDefaultAsync(a => a.Slug == axisSlug);
        if (axis == null) throw new KeyNotFoundException($"Osa '{axisSlug}' nebyla nalezena.");

        var entries = await _context.ChronicleAxisEntries.Where(e => e.AxisId == axis.Id).ToListAsync();

        List<int>? allowedRegionIds = null;
        if (filter.RegionIds is { Count: > 0 })
            allowedRegionIds = await _regionService.ExpandToDescendants(filter.RegionIds);

        var expandedCategoryIds = new HashSet<int>(filter.ExpandedCategoryIds ?? new List<int>());
        var nodes = new List<ChronicleTimelineNode>();

        // Direct item entries.
        var directItemIds = entries.Where(e => e.ItemId.HasValue).Select(e => e.ItemId!.Value).ToList();
        if (directItemIds.Count > 0)
        {
            var items = await LoadItems(i => directItemIds.Contains(i.Id));
            nodes.AddRange(items.Where(i => PassesFilter(i, filter, allowedRegionIds)).Select(ToNode));
        }

        // Category entries: expanded (per-axis ExpandChildren, or client's runtime "Zobrazit na ose" toggle)
        // render every contained item individually; collapsed ones render as one pseudo-node (doc §4).
        foreach (var entry in entries.Where(e => e.CategoryId.HasValue))
        {
            var categoryId = entry.CategoryId!.Value;
            var expand = entry.ExpandChildren || expandedCategoryIds.Contains(categoryId);
            var descendantIds = await _categoryService.GetDescendantIds(categoryId);

            var items = await LoadItems(i => i.Categories.Any(c => descendantIds.Contains(c.CategoryId)));
            var filtered = items.Where(i => PassesFilter(i, filter, allowedRegionIds)).ToList();
            if (filtered.Count == 0) continue;

            if (expand)
            {
                nodes.AddRange(filtered.Select(ToNode));
            }
            else
            {
                var category = await _context.ChronicleCategories.FindAsync(categoryId);
                if (category != null) nodes.Add(ToCategoryNode(category, filtered));
            }
        }

        // Era grouping (collapsed "Pravěk · N položek" blocks) is turned off: every item just
        // renders in the flat chronological list, no special era rows at all.
        var result = ChronicleLayout.Compute(nodes);

        var response = new ChronicleLayoutResponse
        {
            AxisId = axis.Id,
            AxisSlug = axis.Slug,
            Cards = result.Cards,
            Markers = result.Markers,
            RowLabels = result.RowLabels,
            TotalHeight = result.TotalHeight
        };

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
        return response;
    }

    private async Task<List<ChronicleItem>> LoadItems(Expression<Func<ChronicleItem, bool>> predicate) =>
        await _context.ChronicleItems
            .Include(i => i.Categories).ThenInclude(c => c.Category)
            .Include(i => i.Regions).ThenInclude(r => r.Region)
            .Include(i => i.Sources)
            .Include(i => i.MinGrade)
            .AsSplitQuery()
            .Where(predicate)
            .ToListAsync();

    private static bool PassesFilter(ChronicleItem item, ChronicleFilter filter, List<int>? allowedRegionIds)
    {
        if (item is ChroniclePerson)
        {
            if (!filter.IncludePeople && !filter.PeopleOnly) return false;
        }
        else if (filter.PeopleOnly)
        {
            return false;
        }

        if (filter.MinImportance.HasValue && item.Importance < filter.MinImportance.Value) return false;
        if (filter.MaxGradeLevel.HasValue && item.MinGrade != null && item.MinGrade.Level > filter.MaxGradeLevel.Value) return false;
        if (allowedRegionIds != null && !item.Regions.Any(r => allowedRegionIds.Contains(r.RegionId))) return false;

        return true;
    }

    private static ChronicleTimelineNode ToNode(ChronicleItem item)
    {
        var response = ChronicleMapper.ToResponse(item);
        return new ChronicleTimelineNode($"item-{item.Id}", response, item.StartYear, item.EndYear, response.PrimaryColor);
    }

    private static ChronicleTimelineNode ToCategoryNode(ChronicleCategory category, List<ChronicleItem> items)
    {
        var start = items.Min(i => i.StartYear);
        var end = items.Select(i => i.EndYear ?? i.StartYear).Max();
        var effectiveEnd = end == start ? (long?)null : end;

        var pseudo = new ChronicleItemResponse
        {
            Id = category.Id,
            Slug = $"category-{category.Id}",
            ItemType = ChronicleLayoutNodeTypes.Category,
            Title = category.Name,
            Summary = category.Summary,
            StartYear = start,
            EndYear = effectiveEnd,
            PrimaryColor = category.Color
        };
        return new ChronicleTimelineNode($"category-{category.Id}", pseudo, start, effectiveEnd, category.Color, items.Count);
    }
}
