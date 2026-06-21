using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IEventService
{
    Task<List<EventResponse>> GetAll();
    Task<List<EventResponse>> GetByDateRange(DateTime startDate, DateTime endDate);
    Task<EventResponse?> GetById(int id);
    Task<EventResponse?> Create(CreateEventRequest request);
    Task<bool> Update(int id, UpdateEventRequest request);
    Task<bool> Delete(int id);
    Task<List<EventResponse>> Search(string searchTerm);
}

public class EventService : IEventService
{
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
            var events = await _context.Events
                .AsNoTracking()
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

    public async Task<List<EventResponse>> GetByDateRange(DateTime startDate, DateTime endDate)
    {
        try
        {
            var events = await _context.Events
                .AsNoTracking()
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

    public async Task<EventResponse?> GetById(int id)
    {
        var eventEntity = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
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
                Color = request.Color,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                RecurrenceEndDate = request.RecurrenceEndDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            return MapToResponse(eventEntity);
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
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null) return false;

            eventEntity.Title = request.Title;
            eventEntity.Description = request.Description;
            eventEntity.StartDate = request.StartDate;
            eventEntity.EndDate = request.EndDate;
            eventEntity.IsAllDay = request.IsAllDay;
            eventEntity.Location = request.Location;
            eventEntity.Color = request.Color;
            eventEntity.IsRecurring = request.IsRecurring;
            eventEntity.RecurrencePattern = request.RecurrencePattern;
            eventEntity.RecurrenceEndDate = request.RecurrenceEndDate;
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
            var events = await _context.Events
                .AsNoTracking()
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

    private static EventResponse MapToResponse(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            IsAllDay = eventEntity.IsAllDay,
            Location = eventEntity.Location,
            Color = eventEntity.Color,
            IsRecurring = eventEntity.IsRecurring,
            RecurrencePattern = eventEntity.RecurrencePattern,
            RecurrenceEndDate = eventEntity.RecurrenceEndDate
        };
    }

    private static List<EventResponse> GenerateRecurringInstances(Event eventEntity, DateTime rangeStart, DateTime rangeEnd)
    {
        var instances = new List<EventResponse>();

        if (string.IsNullOrEmpty(eventEntity.RecurrencePattern))
            return instances;

        var currentDate = eventEntity.StartDate;
        var eventDuration = eventEntity.EndDate?.Subtract(eventEntity.StartDate) ?? TimeSpan.Zero;

        while (currentDate < rangeStart &&
               (eventEntity.RecurrenceEndDate == null || currentDate <= eventEntity.RecurrenceEndDate))
        {
            currentDate = GetNextOccurrence(currentDate, eventEntity.RecurrencePattern);
        }

        while (currentDate <= rangeEnd &&
               (eventEntity.RecurrenceEndDate == null || currentDate <= eventEntity.RecurrenceEndDate))
        {
            var instanceEndDate = eventEntity.EndDate?.Add(currentDate.Subtract(eventEntity.StartDate));

            instances.Add(new EventResponse
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                StartDate = currentDate,
                EndDate = instanceEndDate,
                IsAllDay = eventEntity.IsAllDay,
                Location = eventEntity.Location,
                Color = eventEntity.Color,
                IsRecurring = eventEntity.IsRecurring,
                RecurrencePattern = eventEntity.RecurrencePattern,
                RecurrenceEndDate = eventEntity.RecurrenceEndDate
            });

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
