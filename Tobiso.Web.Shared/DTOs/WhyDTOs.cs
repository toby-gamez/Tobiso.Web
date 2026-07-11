namespace Tobiso.Web.Shared.DTOs;

public class WhyRequest
{
    public int PostId { get; set; }
    public string Sentence { get; set; } = string.Empty;
}

public class WhyResponse
{
    public string Explanation { get; set; } = string.Empty;
}

public class KeyTermEntry
{
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}
