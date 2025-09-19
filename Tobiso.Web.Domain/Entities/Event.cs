namespace Tobiso.Web.Domain.Entities;

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
}