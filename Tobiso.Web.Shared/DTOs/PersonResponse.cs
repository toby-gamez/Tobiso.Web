namespace Tobiso.Web.Shared.DTOs;

public class PersonResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Bio { get; set; }
    public string? Role { get; set; }
    public int? BirthYear { get; set; }
    public int? DeathYear { get; set; }
    public string? ExternalLink { get; set; }
    public string? PhotoUrl { get; set; }
    public bool AiGenerated { get; set; }
}
