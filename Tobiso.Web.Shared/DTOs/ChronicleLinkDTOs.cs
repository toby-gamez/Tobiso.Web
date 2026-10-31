using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public class ChronicleLinkResponse
{
    public int Id { get; set; }
    public int FromId { get; set; }
    public string FromTitle { get; set; } = string.Empty;
    public string FromSlug { get; set; } = string.Empty;
    public int ToId { get; set; }
    public string ToTitle { get; set; } = string.Empty;
    public string ToSlug { get; set; } = string.Empty;
    public ChronicleLinkType Type { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class CreateChronicleLinkRequest
{
    public int FromId { get; set; }
    public int ToId { get; set; }
    public ChronicleLinkType Type { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
