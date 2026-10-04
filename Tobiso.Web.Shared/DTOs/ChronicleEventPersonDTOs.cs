using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class ChronicleEventPersonResponse
{
    public int EventId { get; set; }
    public int PersonId { get; set; }
    public string PersonTitle { get; set; } = string.Empty;
    public string PersonSlug { get; set; } = string.Empty;
    public ChroniclePersonRole Role { get; set; }
    public string? Note { get; set; }
}

public class SetChronicleEventPersonRequest
{
    public int EventId { get; set; }
    public int PersonId { get; set; }
    public ChroniclePersonRole Role { get; set; }
    public string? Note { get; set; }
}
