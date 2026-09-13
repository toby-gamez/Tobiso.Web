using System.Security.Claims;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Services;

// Whether a caller may make one more free (non-credit-charged) AI request right now, and how
// many are left. Populated by IAiUsageGuard.TryConsumeAsync — the check and the consumption
// happen together so callers can't race a separate "check" and "consume" step.
public record AiUsageDecision(bool Allowed, int Remaining, string? DeniedMessage, bool AnonymousLimitReached);

public interface IAiUsageGuard
{
    /// <param name="user">The caller's principal, or null/unauthenticated for an anonymous visitor.</param>
    /// <param name="anonymousKey">
    /// A stable identity for an anonymous caller — the persistent per-browser device id for
    /// Blazor-originated calls, or the IP/X-Device-Id-derived key AiController already computes
    /// for raw HTTP callers. Ignored for authenticated callers (who are keyed by account id
    /// instead), but always used to look up any purchased bonus quota.
    /// </param>
    Task<AiUsageDecision> TryConsumeAsync(ClaimsPrincipal? user, string anonymousKey, string? clientId = null);
}

// Single place that decides whether a free AI request is allowed — used by both AiController
// (the HTTP surface hit by the mobile/admin apps) and the Blazor Server pages that call AiService
// directly in-process (AiChatBox, PracticeAi, PostDetail). Because Blazor Server components run
// server-side over a persistent circuit rather than per-request HTTP, they don't have a reliable
// client IP to key on — every caller here is identified by account id (authenticated) or an
// explicit anonymous key the caller supplies (device id), never by inspecting HttpContext.
public class AiUsageGuard : IAiUsageGuard
{
    private readonly IAiRateLimitService _rateLimitService;
    private readonly IAnonymousUsageService _anonymousUsage;
    private readonly IConfiguration _configuration;

    public AiUsageGuard(IAiRateLimitService rateLimitService, IAnonymousUsageService anonymousUsage, IConfiguration configuration)
    {
        _rateLimitService = rateLimitService;
        _anonymousUsage = anonymousUsage;
        _configuration = configuration;
    }

    public async Task<AiUsageDecision> TryConsumeAsync(ClaimsPrincipal? user, string anonymousKey, string? clientId = null)
    {
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (user?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId))
        {
            // Renews daily — renewal is a benefit of having an account. Keyed by account id
            // (not IP), so it works for Blazor-originated calls and isn't shared across
            // everyone on the same network.
            var key = $"user:{userId}";
            int baseLimit;
            if (!string.IsNullOrEmpty(clientId))
            {
                var confVal = _configuration[$"OpenAI:ClientLimits:{clientId}"];
                baseLimit = !string.IsNullOrEmpty(confVal) && int.TryParse(confVal, out var cl) ? cl
                    : int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            }
            else
            {
                baseLimit = int.TryParse(_configuration["OpenAI:MaxDailyRequests"], out var l) ? l : 10;
            }

            var effectiveLimit = baseLimit + _rateLimitService.GetBonusTotal(anonymousKey);
            var allowed = _rateLimitService.TryConsume(key, effectiveLimit);
            var remaining = _rateLimitService.GetRemaining(key, effectiveLimit);
            return new AiUsageDecision(allowed, remaining,
                allowed ? null : "Denní limit dotazů byl vyčerpán.", false);
        }

        // Anonymous: a fixed lifetime allowance that never renews — "N requests, ever" — plus
        // any purchased bonus quota on top.
        var anonBase = int.TryParse(_configuration["OpenAI:AnonymousLifetimeRequests"], out var al) ? al : 20;
        var anonLimit = anonBase + _rateLimitService.GetBonusTotal(anonymousKey);
        var anonAllowed = await _anonymousUsage.TryConsumeAsync(anonymousKey, anonLimit);
        var anonRemaining = await _anonymousUsage.GetRemainingAsync(anonymousKey, anonLimit);
        return new AiUsageDecision(anonAllowed, anonRemaining,
            anonAllowed ? null : "Vyčerpal jsi všechny AI dotazy zdarma. Zaregistruj se a získej dalších 20 kreditů.",
            !anonAllowed);
    }
}
