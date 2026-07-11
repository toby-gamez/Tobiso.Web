namespace Tobiso.Web.Shared.DTOs;

public class CompareRequest
{
    public int PostId { get; set; }
    public string CompareTo { get; set; } = string.Empty;
}

public class CompareResponse
{
    public string MarkdownTable { get; set; } = string.Empty;
}
