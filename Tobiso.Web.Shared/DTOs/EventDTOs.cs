using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class EventResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    // The color to draw: the admin's own pick, or the type's color when none was picked.
    public string Color { get; set; } = "#007bff";
    // The admin's own pick (null = follows the type), so the edit form knows which one it is.
    public string? CustomColor { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public EventType Type { get; set; }
    public EventScope Scope { get; set; }
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    // Set when a student added the event (null = admin), so the calendar can say who it was.
    public int? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public List<EventThemeDto> Themes { get; set; } = [];
}

/// <summary>A theme (Téma) of an event: a post or a category. Exactly one of PostId / CategoryId is set.</summary>
public class EventThemeDto
{
    public int? PostId { get; set; }
    public int? CategoryId { get; set; }
    // Filled on responses only (ignored on requests).
    public string Name { get; set; } = string.Empty;
    public string? PostFilePath { get; set; }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    // Null = use the type's color.
    public string? Color { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public EventType Type { get; set; } = EventType.DayOff;
    public EventScope Scope { get; set; } = EventScope.Global;
    public int? ClassId { get; set; }
    public int? GroupId { get; set; }
    public List<EventThemeDto> Themes { get; set; } = [];
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    // Null = use the type's color.
    public string? Color { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public EventType Type { get; set; } = EventType.DayOff;
    public EventScope Scope { get; set; } = EventScope.Global;
    public int? ClassId { get; set; }
    public int? GroupId { get; set; }
    public List<EventThemeDto> Themes { get; set; } = [];
}

public class EventsQueryRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
}

/// <summary>An event a student adds from the calendar. Scope Global isn't allowed for students.</summary>
public class StudentEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EventType Type { get; set; } = EventType.Test;
    public EventScope Scope { get; set; } = EventScope.Personal;
    public int? GroupId { get; set; }
    public List<EventThemeDto> Themes { get; set; } = [];
}

public enum StudentEventResult { Ok, NotFound, Forbidden, Invalid, TooMany }
