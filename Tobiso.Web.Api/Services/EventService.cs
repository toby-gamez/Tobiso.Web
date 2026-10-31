using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IEventService
{
    // Public reads: a null viewer (anonymous / no class) sees only global events, as before classes.
    Task<List<EventResponse>> GetAll();
    Task<List<EventResponse>> GetByDateRange(DateTime startDate, DateTime endDate, EventViewer? viewer = null);
    Task<EventResponse?> GetById(int id, EventViewer? viewer = null);
    Task<List<EventResponse>> Search(string searchTerm);

    // Calendar feed: every event the viewer can see that hasn't ended before 'since', recurring
    // ones left unexpanded (the feed emits an RRULE instead).
    Task<List<EventResponse>> GetFeedEvents(EventViewer viewer, DateTime since);

    // Admin: sees and edits every event, including the ones students added.
    Task<List<EventResponse>> GetAllForAdmin();
    Task<EventResponse?> Create(CreateEventRequest request);
    Task<bool> Update(int id, UpdateEventRequest request);
    Task<bool> Delete(int id);

    // Students: create events for themselves / their class / their group; edit only their own.
    Task<(StudentEventResult Result, EventResponse? Event)> CreateStudentEvent(EventViewer viewer, StudentEventRequest request);
    Task<StudentEventResult> UpdateStudentEvent(EventViewer viewer, int id, StudentEventRequest request);
    Task<StudentEventResult> DeleteStudentEvent(EventViewer viewer, int id);
}

public class EventService : IEventService
{
    // Guards against a student flooding their class calendar.
    public const int MaxStudentEventsPerDay = 30;

    private readonly TobisoDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(TobisoDbContext context, ILogger<EventService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<EventResponse>> GetAll()
    {
        try
        {
            var events = await WithDetails(VisibleTo(_context.Events, null))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            return events.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání událostí");
            throw;
        }
    }

    public async Task<List<EventResponse>> GetAllForAdmin()
    {
        var events = await WithDetails(_context.Events)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        return events.Select(MapToResponse).ToList();
    }

    public async Task<List<EventResponse>> GetByDateRange(DateTime startDate, DateTime endDate, EventViewer? viewer = null)
    {
        try
        {
            var events = await WithDetails(VisibleTo(_context.Events, viewer))
                .Where(e => (e.StartDate <= endDate &&
                           (e.EndDate == null || e.EndDate >= startDate)) ||
                           (e.IsRecurring &&
                           (e.RecurrenceEndDate == null || e.RecurrenceEndDate >= startDate) &&
                           e.StartDate <= endDate))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            var eventResponses = new List<EventResponse>();

            foreach (var eventEntity in events)
            {
                if (!eventEntity.IsRecurring)
                {
                    eventResponses.Add(MapToResponse(eventEntity));
                }
                else
                {
                    var instances = GenerateRecurringInstances(eventEntity, startDate, endDate);
                    eventResponses.AddRange(instances);
                }
            }

            return eventResponses.OrderBy(e => e.StartDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání událostí podle data {Start} – {End}", startDate, endDate);
            throw;
        }
    }

    public async Task<List<EventResponse>> GetFeedEvents(EventViewer viewer, DateTime since)
    {
        var events = await WithDetails(VisibleTo(_context.Events, viewer))
            .Where(e => e.StartDate >= since ||
                        (e.EndDate != null && e.EndDate >= since) ||
                        (e.IsRecurring && (e.RecurrenceEndDate == null || e.RecurrenceEndDate >= since)))
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        return events.Select(MapToResponse).ToList();
    }

    public async Task<EventResponse?> GetById(int id, EventViewer? viewer = null)
    {
        var eventEntity = await WithDetails(VisibleTo(_context.Events, viewer)).FirstOrDefaultAsync(e => e.Id == id);
        return eventEntity == null ? null : MapToResponse(eventEntity);
    }

    public async Task<EventResponse?> Create(CreateEventRequest request)
    {
        try
        {
            var eventEntity = new Event
            {
                Title = request.Title,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsAllDay = request.IsAllDay,
                Location = request.Location,
                Color = NormalizeColor(request.Color),
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                RecurrenceEndDate = request.RecurrenceEndDate,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            };
            if (!await TryApplyAdminScope(eventEntity, request.Scope, request.ClassId, request.GroupId))
                return null;

            if (!await TryApplyThemes(eventEntity, request.Type, request.Themes))
                return null;

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            return await GetByIdForAdmin(eventEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření události");
            throw;
        }
    }

    public async Task<bool> Update(int id, UpdateEventRequest request)
    {
        try
        {
            var eventEntity = await _context.Events.Include(e => e.Themes).FirstOrDefaultAsync(e => e.Id == id);
            if (eventEntity == null) return false;
            // Scope first: a rejected scope must not leave half-applied edits tracked on the context.
            if (!await TryApplyAdminScope(eventEntity, request.Scope, request.ClassId, request.GroupId))
                return false;
            if (!await TryApplyThemes(eventEntity, request.Type, request.Themes))
                return false;

            eventEntity.Title = request.Title;
            eventEntity.Description = request.Description;
            eventEntity.StartDate = request.StartDate;
            eventEntity.EndDate = request.EndDate;
            eventEntity.IsAllDay = request.IsAllDay;
            eventEntity.Location = request.Location;
            eventEntity.Color = NormalizeColor(request.Color);
            eventEntity.IsRecurring = request.IsRecurring;
            eventEntity.RecurrencePattern = request.RecurrencePattern;
            eventEntity.RecurrenceEndDate = request.RecurrenceEndDate;
            eventEntity.Type = request.Type;
            eventEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci události {EventId}", id);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null) return false;

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání události {EventId}", id);
            return false;
        }
    }

    public async Task<List<EventResponse>> Search(string searchTerm)
    {
        try
        {
            var events = await WithDetails(VisibleTo(_context.Events, null))
                .Where(e => e.Title.Contains(searchTerm) ||
                           (e.Description != null && e.Description.Contains(searchTerm)) ||
                           (e.Location != null && e.Location.Contains(searchTerm)))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            return events.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vyhledávání událostí: {Term}", searchTerm);
            throw;
        }
    }

    public async Task<(StudentEventResult Result, EventResponse? Event)> CreateStudentEvent(EventViewer viewer, StudentEventRequest request)
    {
        var since = DateTime.UtcNow.AddDays(-1);
        if (await _context.Events.CountAsync(e => e.CreatedByUserId == viewer.UserId && e.CreatedAt >= since) >= MaxStudentEventsPerDay)
            return (StudentEventResult.TooMany, null);

        var eventEntity = new Event { CreatedByUserId = viewer.UserId, CreatedAt = DateTime.UtcNow };
        var result = ApplyStudentRequest(eventEntity, viewer, request);
        if (result != StudentEventResult.Ok) return (result, null);
        if (!await TryApplyStudentThemes(eventEntity, request)) return (StudentEventResult.Invalid, null);

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();
        return (StudentEventResult.Ok, await GetByIdForAdmin(eventEntity.Id));
    }

    public async Task<StudentEventResult> UpdateStudentEvent(EventViewer viewer, int id, StudentEventRequest request)
    {
        var eventEntity = await _context.Events.Include(e => e.Themes).FirstOrDefaultAsync(e => e.Id == id);
        if (eventEntity == null) return StudentEventResult.NotFound;
        if (eventEntity.CreatedByUserId != viewer.UserId) return StudentEventResult.Forbidden;

        var result = ApplyStudentRequest(eventEntity, viewer, request);
        if (result != StudentEventResult.Ok) return result;
        if (!await TryApplyStudentThemes(eventEntity, request)) return StudentEventResult.Invalid;

        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return StudentEventResult.Ok;
    }

    public async Task<StudentEventResult> DeleteStudentEvent(EventViewer viewer, int id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity == null) return StudentEventResult.NotFound;
        if (eventEntity.CreatedByUserId != viewer.UserId) return StudentEventResult.Forbidden;

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync();
        return StudentEventResult.Ok;
    }

    private const int MaxStudentThemes = 10;

    private Task<bool> TryApplyStudentThemes(Event eventEntity, StudentEventRequest request) =>
        request.Themes?.Count > MaxStudentThemes
            ? Task.FromResult(false)
            : TryApplyThemes(eventEntity, request.Type, request.Themes);

    // Student events are whole-day, non-recurring and coloured by type. Class/group scope needs an
    // approved membership (and for a group, being in that group) - checked against the viewer the
    // caller resolved from the database, never against ids sent by the client. Nothing on the
    // entity changes unless the whole request is valid.
    private static StudentEventResult ApplyStudentRequest(Event eventEntity, EventViewer viewer, StudentEventRequest request)
    {
        var title = request.Title?.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > 200 || description?.Length > 1000)
            return StudentEventResult.Invalid;

        var start = request.StartDate.Date;
        var end = request.EndDate?.Date;
        if (end != null && (end < start || end.Value.Subtract(start).TotalDays > 60))
            return StudentEventResult.Invalid;

        int? classId = null, groupId = null;
        switch (request.Scope)
        {
            case EventScope.Personal:
                break;
            case EventScope.Class:
                if (viewer.ClassId == null) return StudentEventResult.Forbidden;
                classId = viewer.ClassId;
                break;
            case EventScope.Group:
                if (viewer.ClassId == null || request.GroupId == null || !viewer.GroupIds.Contains(request.GroupId.Value))
                    return StudentEventResult.Forbidden;
                classId = viewer.ClassId;
                groupId = request.GroupId;
                break;
            default:
                return StudentEventResult.Forbidden;
        }

        eventEntity.Title = title;
        eventEntity.Description = description;
        eventEntity.StartDate = start;
        eventEntity.EndDate = end == start ? null : end;
        eventEntity.IsAllDay = true;
        eventEntity.Location = null;
        eventEntity.IsRecurring = false;
        eventEntity.RecurrencePattern = null;
        eventEntity.RecurrenceEndDate = null;
        eventEntity.Type = request.Type;
        eventEntity.Color = null;
        eventEntity.Scope = request.Scope;
        eventEntity.ClassId = classId;
        eventEntity.GroupId = groupId;
        return StudentEventResult.Ok;
    }

    // Admin may put an event on any class or group. Personal scope only makes sense for an event a
    // student already owns (the admin is editing it), so it can't be chosen for a new one.
    private async Task<bool> TryApplyAdminScope(Event eventEntity, EventScope scope, int? classId, int? groupId)
    {
        switch (scope)
        {
            case EventScope.Global:
                eventEntity.ClassId = null;
                eventEntity.GroupId = null;
                break;
            case EventScope.Class:
                // A null classId means "all classes": visible to every class member, not to the public.
                if (classId != null && !await _context.SchoolClasses.AnyAsync(c => c.Id == classId)) return false;
                eventEntity.ClassId = classId;
                eventEntity.GroupId = null;
                break;
            case EventScope.Group:
                var group = groupId == null ? null : await _context.ClassGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
                if (group == null) return false;
                eventEntity.ClassId = group.ClassId;
                eventEntity.GroupId = group.Id;
                break;
            case EventScope.Personal:
                if (eventEntity.CreatedByUserId == null) return false;
                eventEntity.ClassId = null;
                eventEntity.GroupId = null;
                break;
            default:
                return false;
        }

        eventEntity.Scope = scope;
        return true;
    }

    // Replaces the event's themes with the requested ones. Every post / category must exist and each
    // entry names exactly one of them; duplicates are dropped. Only tests carry themes.
    private async Task<bool> TryApplyThemes(Event eventEntity, EventType type, List<EventThemeDto>? themes)
    {
        themes = type == EventType.Test ? themes ?? [] : [];
        if (themes.Any(t => (t.PostId == null) == (t.CategoryId == null)))
            return false;

        var postIds = themes.Where(t => t.PostId != null).Select(t => t.PostId!.Value).Distinct().ToList();
        var categoryIds = themes.Where(t => t.CategoryId != null).Select(t => t.CategoryId!.Value).Distinct().ToList();
        if (await _context.Posts.CountAsync(p => postIds.Contains(p.Id)) != postIds.Count) return false;
        if (await _context.Categories.CountAsync(c => categoryIds.Contains(c.Id)) != categoryIds.Count) return false;

        _context.EventThemes.RemoveRange(eventEntity.Themes);
        eventEntity.Themes = [
            .. postIds.Select(id => new EventTheme { PostId = id }),
            .. categoryIds.Select(id => new EventTheme { CategoryId = id })
        ];
        return true;
    }

    // Blank = follow the type's color.
    private static string? NormalizeColor(string? color) =>
        string.IsNullOrWhiteSpace(color) ? null : color.Trim();

    private async Task<EventResponse?> GetByIdForAdmin(int id)
    {
        var eventEntity = await WithDetails(_context.Events).FirstOrDefaultAsync(e => e.Id == id);
        return eventEntity == null ? null : MapToResponse(eventEntity);
    }

    private static IQueryable<Event> WithDetails(IQueryable<Event> query) =>
        query.AsNoTracking()
            .Include(e => e.Class)
            .Include(e => e.Group)
            .Include(e => e.CreatedByUser)
            .Include(e => e.Themes).ThenInclude(t => t.Post)
            .Include(e => e.Themes).ThenInclude(t => t.Category);

    private static IQueryable<Event> VisibleTo(IQueryable<Event> query, EventViewer? viewer)
    {
        if (viewer == null)
            return query.Where(e => e.Scope == EventScope.Global);

        var userId = viewer.UserId;
        var classId = viewer.ClassId;
        var groupIds = viewer.GroupIds.ToList();
        return query.Where(e =>
            e.Scope == EventScope.Global ||
            (e.Scope == EventScope.Class && classId != null && (e.ClassId == null || e.ClassId == classId)) ||
            (e.Scope == EventScope.Group && e.GroupId != null && groupIds.Contains(e.GroupId.Value)) ||
            (e.Scope == EventScope.Personal && e.CreatedByUserId == userId));
    }

    private static EventResponse MapToResponse(Event eventEntity) =>
        MapToResponse(eventEntity, eventEntity.StartDate, eventEntity.EndDate);

    private static EventResponse MapToResponse(Event eventEntity, DateTime startDate, DateTime? endDate)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = startDate,
            EndDate = endDate,
            IsAllDay = eventEntity.IsAllDay,
            Location = eventEntity.Location,
            Color = eventEntity.EffectiveColor,
            CustomColor = eventEntity.Color,
            IsRecurring = eventEntity.IsRecurring,
            RecurrencePattern = eventEntity.RecurrencePattern,
            RecurrenceEndDate = eventEntity.RecurrenceEndDate,
            Type = eventEntity.Type,
            Scope = eventEntity.Scope,
            ClassId = eventEntity.ClassId,
            ClassName = eventEntity.Class?.Name,
            GroupId = eventEntity.GroupId,
            GroupName = eventEntity.Group?.Name,
            CreatedByUserId = eventEntity.CreatedByUserId,
            CreatedByName = eventEntity.CreatedByUser?.DisplayName,
            Themes = eventEntity.Themes.Select(t => new EventThemeDto
            {
                PostId = t.PostId,
                CategoryId = t.CategoryId,
                Name = t.Post?.Title ?? t.Category?.Name ?? string.Empty,
                PostFilePath = t.Post?.FilePath
            }).ToList()
        };
    }

    private static List<EventResponse> GenerateRecurringInstances(Event eventEntity, DateTime rangeStart, DateTime rangeEnd)
    {
        var instances = new List<EventResponse>();

        if (string.IsNullOrEmpty(eventEntity.RecurrencePattern))
            return instances;

        var currentDate = eventEntity.StartDate;

        while (currentDate < rangeStart &&
               (eventEntity.RecurrenceEndDate == null || currentDate <= eventEntity.RecurrenceEndDate))
        {
            currentDate = GetNextOccurrence(currentDate, eventEntity.RecurrencePattern);
        }

        while (currentDate <= rangeEnd &&
               (eventEntity.RecurrenceEndDate == null || currentDate <= eventEntity.RecurrenceEndDate))
        {
            var instanceEndDate = eventEntity.EndDate?.Add(currentDate.Subtract(eventEntity.StartDate));
            instances.Add(MapToResponse(eventEntity, currentDate, instanceEndDate));
            currentDate = GetNextOccurrence(currentDate, eventEntity.RecurrencePattern);
        }

        return instances;
    }

    private static DateTime GetNextOccurrence(DateTime currentDate, string recurrencePattern)
    {
        return recurrencePattern.ToLowerInvariant() switch
        {
            "daily" => currentDate.AddDays(1),
            "weekly" => currentDate.AddDays(7),
            "monthly" => currentDate.AddMonths(1),
            "yearly" => currentDate.AddYears(1),
            _ => currentDate.AddDays(7)
        };
    }
}
