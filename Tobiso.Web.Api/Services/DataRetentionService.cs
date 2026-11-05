using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;

namespace Tobiso.Web.Api.Services;

/// <summary>Retention periods (GDPR Art. 5(1)(e) storage limitation). 0 disables a rule.</summary>
public record DataRetentionOptions
{
    /// <summary>Accounts whose email was never confirmed (the verification link lives 48 h).</summary>
    public int UnverifiedAccountDays { get; init; } = 7;
    /// <summary>Per-device anonymous AI counters - only needed for the current day's quota.</summary>
    public int AnonymousAiUsageDays { get; init; } = 7;
    /// <summary>Push device tokens not seen for this long (FCM treats them as stale anyway).</summary>
    public int DeviceTokenDays { get; init; } = 180;
    /// <summary>Feedback (name, email, message) after it was submitted.</summary>
    public int FeedbackDays { get; init; } = 730;
    /// <summary>AI chat sessions not touched for this long. Off by default - set once the privacy policy states a period.</summary>
    public int AiChatDays { get; init; }
    /// <summary>Accounts with no login for this long are deleted. Off by default - should be announced (and ideally warned by email) first.</summary>
    public int InactiveAccountDays { get; init; }

    public static DataRetentionOptions FromConfiguration(IConfiguration config) =>
        config.GetSection("Retention").Get<DataRetentionOptions>() ?? new DataRetentionOptions();
}

public record DataRetentionResult(int UnverifiedAccounts, int AnonymousAiUsages, int DeviceTokens, int Feedback, int AiChats, int InactiveAccounts);

public interface IDataRetentionService
{
    Task<DataRetentionResult> RunAsync(DateTime utcNow, CancellationToken ct = default);
}

/// <summary>
/// Deletes personal data that has outlived its purpose. Deleting a user cascades to all of their
/// rows (chats, notes, bookmarks, progress, credit log, class membership), as in self-service deletion.
/// </summary>
public class DataRetentionService(IDbContextFactory<TobisoDbContext> dbFactory, IConfiguration config,
    ILogger<DataRetentionService> logger) : IDataRetentionService
{
    public async Task<DataRetentionResult> RunAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var o = DataRetentionOptions.FromConfiguration(config);
        await using var db = dbFactory.CreateDbContext();

        // Computed up front: EF translates captured variables, not method calls on them.
        var unverifiedCutoff = utcNow.AddDays(-o.UnverifiedAccountDays);
        var anonCutoff = utcNow.AddDays(-o.AnonymousAiUsageDays);
        var deviceCutoff = utcNow.AddDays(-o.DeviceTokenDays);
        var feedbackCutoff = utcNow.AddDays(-o.FeedbackDays);
        var chatCutoff = utcNow.AddDays(-o.AiChatDays);
        var inactiveCutoff = utcNow.AddDays(-o.InactiveAccountDays);

        var unverified = o.UnverifiedAccountDays <= 0 ? 0 : await db.Users
            .Where(u => u.EmailConfirmedAt == null && u.CreatedAt < unverifiedCutoff)
            .ExecuteDeleteAsync(ct);

        var anon = o.AnonymousAiUsageDays <= 0 ? 0 : await db.AnonymousAiUsages
            .Where(a => a.LastUsedAt < anonCutoff)
            .ExecuteDeleteAsync(ct);

        // Expired bonus grants can no longer be redeemed or counted (ValidUntil is part of the signed
        // payload, at most 25 h ahead), so their replay-protection rows are no longer needed either.
        var expiredGrantCutoff = utcNow.AddDays(-1);
        await db.AiBonusGrants.Where(g => g.ValidUntil < expiredGrantCutoff).ExecuteDeleteAsync(ct);

        var devices = o.DeviceTokenDays <= 0 ? 0 : await db.DeviceTokens
            .Where(d => d.LastSeenAt < deviceCutoff)
            .ExecuteDeleteAsync(ct);

        var feedback = o.FeedbackDays <= 0 ? 0 : await db.Feedbacks
            .Where(f => f.CreatedAt < feedbackCutoff)
            .ExecuteDeleteAsync(ct);

        var chats = o.AiChatDays <= 0 ? 0 : await db.AiChatSessions
            .Where(s => s.UpdatedAt < chatCutoff)
            .ExecuteDeleteAsync(ct);

        var inactive = o.InactiveAccountDays <= 0 ? 0 : await db.Users
            .Where(u => (u.LastLoginAt ?? u.CreatedAt) < inactiveCutoff)
            .ExecuteDeleteAsync(ct);

        var result = new DataRetentionResult(unverified, anon, devices, feedback, chats, inactive);
        logger.LogInformation("Data retention run: {@Result}", result);
        return result;
    }
}
