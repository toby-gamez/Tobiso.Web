using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tobiso.Web.Api.Services;

public interface IEmailSender
{
    /// <summary>Throws if the message can't be handed to the mail server.</summary>
    Task SendAsync(string to, string subject, string textBody);
}

/// <summary>
/// Plain SMTP using the <c>Email</c> config section (SmtpHost, SmtpPort, SmtpUser, SmtpPass, From,
/// EnableSsl). Without SmtpHost it only works in Development, where the message - including any
/// link in it - is written to the log instead; elsewhere it fails rather than logging live links.
/// </summary>
public class SmtpEmailSender(IConfiguration config, IHostEnvironment env, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string textBody)
    {
        var section = config.GetSection("Email");
        var host = section["SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (env.IsDevelopment())
            {
                logger.LogWarning("Email:SmtpHost not set - development only, logging the email to {To}:\n{Subject}\n{Body}", to, subject, textBody);
                return;
            }
            throw new InvalidOperationException("Email is not configured (Email:SmtpHost).");
        }

        var from = section["From"];
        if (string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email is not configured (Email:From).");

        using var message = new MailMessage(from, to, subject, textBody) { BodyEncoding = System.Text.Encoding.UTF8, SubjectEncoding = System.Text.Encoding.UTF8 };
        using var client = new SmtpClient(host, int.TryParse(section["SmtpPort"], out var port) ? port : 587)
        {
            EnableSsl = !string.Equals(section["EnableSsl"], "false", StringComparison.OrdinalIgnoreCase),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000
        };
        var user = section["SmtpUser"];
        if (!string.IsNullOrEmpty(user))
            client.Credentials = new NetworkCredential(user, section["SmtpPass"]);

        await client.SendMailAsync(message);
    }
}
