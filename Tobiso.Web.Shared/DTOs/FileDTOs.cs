namespace Tobiso.Web.Shared.DTOs;

/// <summary>
/// File upload response - STEJNÝ jako SentrySMP
/// </summary>
public record FileUploadResponse
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
}