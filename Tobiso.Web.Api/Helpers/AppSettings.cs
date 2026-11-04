namespace Tobiso.Api.Helpers;

public class AppSettings
{
    public int RefreshTokenTTL { get; set; }
    public string EmailFrom { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPass { get; set; } = string.Empty;
    public string MainApi { get; set; } = string.Empty;
}
