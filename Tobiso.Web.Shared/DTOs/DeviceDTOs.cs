namespace Tobiso.Web.Shared.DTOs;

public class RegisterDeviceRequest
{
    public string FcmToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
}
