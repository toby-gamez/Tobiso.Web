namespace Tobiso.Web.Domain.Entities;

public class DeviceToken
{
    public int Id { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
