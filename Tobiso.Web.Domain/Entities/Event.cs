using System.Text.Json.Serialization;

namespace Tobiso.Web.Domain.Entities;

/// <summary>What an event is about; <see cref="DayOff"/> is the original "no school" calendar entry.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EventType>))]
public enum EventType { DayOff, Test, Homework, Other }

/// <summary>Who sees an event: everyone, one class, one class group, or only its author.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EventScope>))]
public enum EventScope { Global, Class, Group, Personal }

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    // Null = follow the type's color (see DefaultColor); set only when the admin picks one on purpose.
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // For recurring events
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; } // daily, weekly, monthly, yearly
    public DateTime? RecurrenceEndDate { get; set; }

    public EventType Type { get; set; } = EventType.DayOff;
    public EventScope Scope { get; set; } = EventScope.Global;
    public int? ClassId { get; set; }
    public SchoolClass? Class { get; set; }
    public int? GroupId { get; set; }
    public ClassGroup? Group { get; set; }
    // Null = added by the admin; otherwise the student who added it (shown in the calendar).
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    // Témata: posts / categories the event is about (meant for tests).
    public ICollection<EventTheme> Themes { get; set; } = new List<EventTheme>();

    public string EffectiveColor => Color ?? DefaultColor(Type);

    public static string DefaultColor(EventType type) => type switch
    {
        EventType.Test => "#d9534f",
        EventType.Homework => "#f0ad4e",
        EventType.Other => "#5b8def",
        _ => "#007bff"
    };
}
