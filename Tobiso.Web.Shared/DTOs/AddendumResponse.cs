namespace Tobiso.Web.Shared.DTOs;

public class AddendumResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
