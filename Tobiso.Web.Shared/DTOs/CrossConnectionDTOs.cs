namespace Tobiso.Web.Shared.DTOs;

public class CrossConnection
{
    public string TargetPostTitle { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class CrossConnectionResponse
{
    public List<CrossConnection> Connections { get; set; } = new();
}
