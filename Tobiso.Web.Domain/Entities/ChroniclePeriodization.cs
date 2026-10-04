namespace Tobiso.Web.Domain.Entities;

public class ChroniclePeriodization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<ChroniclePeriod> Periods { get; set; } = new List<ChroniclePeriod>();
}
