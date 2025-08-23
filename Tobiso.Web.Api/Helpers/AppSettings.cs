namespace Tobiso.Api.Helpers;

public class AppSettings
{
    public int RefreshTokenTTL { get; set; }
    public string EmailFrom { get; set; }
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPass { get; set; }
    public string MainApi { get; set; }
}
