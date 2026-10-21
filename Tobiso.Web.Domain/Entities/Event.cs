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
    public string Color { get; set; } = "#007bff"; // Default blue color
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
}
