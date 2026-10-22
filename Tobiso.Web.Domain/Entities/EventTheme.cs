namespace Tobiso.Web.Domain.Entities;

/// <summary>A theme (Téma) of an event, e.g. what a test covers: exactly one of a post or a category.</summary>
public class EventTheme
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}
