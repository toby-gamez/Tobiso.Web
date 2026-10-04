using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IChronicleItemService
{
    Task<ChronicleItemResponse?> GetBySlug(string slug);
    Task<ChronicleItemResponse?> GetById(int id);
    Task<List<ChronicleItemResponse>> GetByIds(IEnumerable<int> ids);
    Task<List<ChronicleItemResponse>> Search(string? query, string? itemType, int take = 25);
    Task<ChronicleItemResponse> Create(CreateChronicleItemRequest request);
    Task<ChronicleItemResponse> Update(int id, UpdateChronicleItemRequest request);
    Task Delete(int id);

    Task<List<ChronicleEventPersonResponse>> GetEventPeople(int eventId);
    Task<List<ChronicleEventPersonResponse>> GetPersonEvents(int personId);
    Task<ChronicleEventPersonResponse> SetEventPerson(SetChronicleEventPersonRequest request);
    Task RemoveEventPerson(int eventId, int personId);

    Task<ChronicleSourceResponse> AddSource(CreateChronicleSourceRequest request);
    Task RemoveSource(int sourceId);
}

public class ChronicleItemService : IChronicleItemService
{
    private readonly TobisoDbContext _context;

    public ChronicleItemService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<ChronicleItem> ItemsWithGraph() =>
        _context.ChronicleItems
            .Include(i => i.Categories).ThenInclude(c => c.Category)
            .Include(i => i.Regions).ThenInclude(r => r.Region)
            .Include(i => i.Sources)
            .Include(i => i.MinGrade);

    public async Task<ChronicleItemResponse?> GetBySlug(string slug)
    {
        var item = await ItemsWithGraph().AsSplitQuery().FirstOrDefaultAsync(i => i.Slug == slug);
        return item == null ? null : ChronicleMapper.ToResponse(item);
    }

    public async Task<ChronicleItemResponse?> GetById(int id)
    {
        var item = await ItemsWithGraph().AsSplitQuery().FirstOrDefaultAsync(i => i.Id == id);
        return item == null ? null : ChronicleMapper.ToResponse(item);
    }

    public async Task<List<ChronicleItemResponse>> GetByIds(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new List<ChronicleItemResponse>();

        var items = await ItemsWithGraph().AsSplitQuery().Where(i => idList.Contains(i.Id)).ToListAsync();
        return items.Select(ChronicleMapper.ToResponse).ToList();
    }

    public async Task<List<ChronicleItemResponse>> Search(string? query, string? itemType, int take = 25)
    {
        var q = ItemsWithGraph().AsQueryable();

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            q = itemType.Equals(ChronicleItemTypeConstants.Person, StringComparison.OrdinalIgnoreCase)
                ? q.Where(i => i is ChroniclePerson)
                : q.Where(i => i is ChronicleEvent);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(i => EF.Functions.Like(i.Title, $"%{term}%") || EF.Functions.Like(i.Slug, $"%{term}%"));
        }

        var items = await q.AsSplitQuery().OrderBy(i => i.StartYear).Take(take).ToListAsync();
        return items.Select(ChronicleMapper.ToResponse).ToList();
    }

    public async Task<ChronicleItemResponse> Create(CreateChronicleItemRequest request)
    {
        await ValidateRequest(request, existingId: null);

        ChronicleItem entity = request.ItemType.Equals(ChronicleItemTypeConstants.Person, StringComparison.OrdinalIgnoreCase)
            ? new ChroniclePerson { Occupation = request.Occupation }
            : new ChronicleEvent();

        ApplyFields(entity, request);
        _context.Add(entity);
        await _context.SaveChangesAsync();

        await SyncCategories(entity.Id, request.CategoryIds, request.PrimaryCategoryId);
        await SyncRegions(entity.Id, request.RegionIds);
        await _context.SaveChangesAsync();

        return (await GetById(entity.Id))!;
    }

    public async Task<ChronicleItemResponse> Update(int id, UpdateChronicleItemRequest request)
    {
        var entity = await _context.ChronicleItems.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Položka kroniky nebyla nalezena.");

        await ValidateRequest(request, existingId: id);

        ApplyFields(entity, request);
        if (entity is ChroniclePerson person) person.Occupation = request.Occupation;
        entity.UpdatedAt = DateTime.UtcNow;

        await SyncCategories(id, request.CategoryIds, request.PrimaryCategoryId);
        await SyncRegions(id, request.RegionIds);
        await _context.SaveChangesAsync();

        return (await GetById(id))!;
    }

    public async Task Delete(int id)
    {
        var entity = await _context.ChronicleItems.FindAsync(id);
        if (entity == null) throw new KeyNotFoundException("Položka kroniky nebyla nalezena.");
        _context.ChronicleItems.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private async Task ValidateRequest(CreateChronicleItemRequest request, int? existingId)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new InvalidOperationException("Slug položky je povinný.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Název položky je povinný.");
        if (request.EndYear.HasValue && request.EndYear.Value < request.StartYear)
            throw new InvalidOperationException("Rok konce nemůže být před rokem začátku.");

        var slugTaken = await _context.ChronicleItems
            .AnyAsync(i => i.Slug == request.Slug && (!existingId.HasValue || i.Id != existingId.Value));
        if (slugTaken)
            throw new InvalidOperationException($"Položka se slugem '{request.Slug}' už existuje.");
    }

    private static void ApplyFields(ChronicleItem entity, CreateChronicleItemRequest request)
    {
        entity.Slug = request.Slug;
        entity.Title = request.Title;
        entity.Summary = request.Summary;
        entity.Content = request.Content;
        entity.StartYear = request.StartYear;
        entity.EndYear = request.EndYear;
        entity.Precision = request.Precision;
        entity.Reliability = request.Reliability;
        entity.ReliabilityNote = request.ReliabilityNote;
        entity.Importance = request.Importance;
        entity.MinGradeId = request.MinGradeId;
        entity.LessonSlug = request.LessonSlug;
    }

    private async Task SyncCategories(int itemId, List<int> categoryIds, int? primaryCategoryId)
    {
        var existing = await _context.ChronicleItemCategories.Where(c => c.ItemId == itemId).ToListAsync();
        _context.ChronicleItemCategories.RemoveRange(existing);

        foreach (var categoryId in categoryIds.Distinct())
        {
            _context.ChronicleItemCategories.Add(new ChronicleItemCategory
            {
                ItemId = itemId,
                CategoryId = categoryId,
                IsPrimary = categoryId == primaryCategoryId
            });
        }
    }

    private async Task SyncRegions(int itemId, List<int> regionIds)
    {
        var existing = await _context.ChronicleItemRegions.Where(r => r.ItemId == itemId).ToListAsync();
        _context.ChronicleItemRegions.RemoveRange(existing);

        foreach (var regionId in regionIds.Distinct())
        {
            _context.ChronicleItemRegions.Add(new ChronicleItemRegion { ItemId = itemId, RegionId = regionId });
        }
    }

    public async Task<List<ChronicleEventPersonResponse>> GetEventPeople(int eventId)
    {
        return await _context.ChronicleEventPersons
            .Where(ep => ep.EventId == eventId)
            .Select(ep => new ChronicleEventPersonResponse
            {
                EventId = ep.EventId,
                PersonId = ep.PersonId,
                PersonTitle = ep.Person!.Title,
                PersonSlug = ep.Person.Slug,
                Role = ep.Role,
                Note = ep.Note
            })
            .ToListAsync();
    }

    public async Task<List<ChronicleEventPersonResponse>> GetPersonEvents(int personId)
    {
        return await _context.ChronicleEventPersons
            .Where(ep => ep.PersonId == personId)
            .Select(ep => new ChronicleEventPersonResponse
            {
                EventId = ep.EventId,
                PersonId = ep.PersonId,
                PersonTitle = ep.Event!.Title,
                PersonSlug = ep.Event.Slug,
                Role = ep.Role,
                Note = ep.Note
            })
            .ToListAsync();
    }

    public async Task<ChronicleEventPersonResponse> SetEventPerson(SetChronicleEventPersonRequest request)
    {
        var eventExists = await _context.ChronicleItems.AnyAsync(i => i.Id == request.EventId && i is ChronicleEvent);
        if (!eventExists) throw new InvalidOperationException("Zadaná událost neexistuje.");
        var personExists = await _context.ChronicleItems.AnyAsync(i => i.Id == request.PersonId && i is ChroniclePerson);
        if (!personExists) throw new InvalidOperationException("Zadaná osobnost neexistuje.");

        var existing = await _context.ChronicleEventPersons
            .FirstOrDefaultAsync(ep => ep.EventId == request.EventId && ep.PersonId == request.PersonId);

        if (existing == null)
        {
            existing = new ChronicleEventPerson { EventId = request.EventId, PersonId = request.PersonId };
            _context.ChronicleEventPersons.Add(existing);
        }
        existing.Role = request.Role;
        existing.Note = request.Note;
        await _context.SaveChangesAsync();

        var person = await _context.ChronicleItems.FindAsync(request.PersonId);
        return new ChronicleEventPersonResponse
        {
            EventId = existing.EventId,
            PersonId = existing.PersonId,
            PersonTitle = person!.Title,
            PersonSlug = person.Slug,
            Role = existing.Role,
            Note = existing.Note
        };
    }

    public async Task RemoveEventPerson(int eventId, int personId)
    {
        var existing = await _context.ChronicleEventPersons
            .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.PersonId == personId);
        if (existing == null) return;
        _context.ChronicleEventPersons.Remove(existing);
        await _context.SaveChangesAsync();
    }

    public async Task<ChronicleSourceResponse> AddSource(CreateChronicleSourceRequest request)
    {
        var itemExists = await _context.ChronicleItems.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists) throw new InvalidOperationException("Zadaná položka neexistuje.");

        var entity = new ChronicleSource { ItemId = request.ItemId, Title = request.Title, Url = request.Url, Note = request.Note };
        _context.ChronicleSources.Add(entity);
        await _context.SaveChangesAsync();

        return new ChronicleSourceResponse { Id = entity.Id, ItemId = entity.ItemId, Title = entity.Title, Url = entity.Url, Note = entity.Note };
    }

    public async Task RemoveSource(int sourceId)
    {
        var entity = await _context.ChronicleSources.FindAsync(sourceId);
        if (entity == null) return;
        _context.ChronicleSources.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
