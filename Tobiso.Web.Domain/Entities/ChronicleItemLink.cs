namespace Tobiso.Web.Domain.Entities;

/// <summary>Causality edge between two kronika items.</summary>
public class ChronicleItemLink
{
    public int Id { get; set; }
    public int FromId { get; set; }
    public ChronicleItem? From { get; set; }
    public int ToId { get; set; }
    public ChronicleItem? To { get; set; }
    public ChronicleLinkType Type { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
