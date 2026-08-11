using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IPushNotificationService
{
    Task SendFeedbackNotificationAsync(Feedback feedback);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(TobisoDbContext context, ILogger<PushNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendFeedbackNotificationAsync(Feedback feedback)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogDebug("Firebase not configured — skipping push notification");
            return;
        }

        var tokens = await _context.DeviceTokens.Select(d => d.FcmToken).ToListAsync();
        if (tokens.Count == 0) return;

        var messages = tokens.Select(token => new Message
        {
            Token = token,
            Data = new Dictionary<string, string>
            {
                ["feedbackId"]   = feedback.Id.ToString(),
                ["feedbackType"] = feedback.Type.ToString(),
                ["platform"]     = feedback.Platform,
                ["title"]        = feedback.Title,
            },
        }).ToList();

        var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);
        _logger.LogInformation("FCM: {Success}/{Total} messages delivered", response.SuccessCount, messages.Count);

        var staleTokens = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess) continue;
            var code = (r.Exception as FirebaseMessagingException)?.MessagingErrorCode;
            _logger.LogWarning("FCM send failed for token #{Index}: {Error}", i, r.Exception?.Message);
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch)
                staleTokens.Add(tokens[i]);
        }

        if (staleTokens.Count > 0)
            await _context.DeviceTokens.Where(d => staleTokens.Contains(d.FcmToken)).ExecuteDeleteAsync();
    }
}
