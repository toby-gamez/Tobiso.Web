namespace Tobiso.Web.Domain.Entities;

public class ChronicleEventPerson
{
    public int EventId { get; set; }
    public ChronicleItem? Event { get; set; }
    public int PersonId { get; set; }
    public ChronicleItem? Person { get; set; }
    public ChroniclePersonRole Role { get; set; }
    public string? Note { get; set; }
}
