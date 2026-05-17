using System.Collections.Generic;

namespace Tobiso.Web.Shared.DTOs;

public class PersonSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public List<string>? Aliases { get; set; }
}
