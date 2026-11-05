using System.Security.Claims;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Services;

// Whether a caller may make one more AI request right now, and how many free ones are left.
// Populated by IAiUsageGuard.TryConsumeAsync - the check and the consumption happen together so
// callers can't race a separate "check" and "consume" step.
public record AiUsageDecision(bool Allowed, int Remaining, string? DeniedMessage, bool AnonymousLimitReached);

public interface IAiUsageGuard
{
    /// <param name="user">The caller's principal, or null/unauthenticated for an anonymous visitor.</param>
    /// <param name="anonymousKey">
    /// The caller's device key ("device:{id}") - the persistent per-browser device id for Blazor
    /// calls, or the X-Device-Id header for raw HTTP callers. It is CLIENT-CONTROLLED, so it only
    /// narrows the allowance (one device can't use up a whole school's IP quota) and looks up any
    /// purchased bonus; it is never the only limit - see the per-IP and global caps below.
    /// </param>
    /// <param name="clientIp">
    /// Server-observed client IP. When null the guard reads it from the current HttpContext
    /// (works for HTTP requests and, in practice, for Blazor Server circuits).
    /// </param>
    Task<AiUsageDecision> TryConsumeAsync(ClaimsPrincipal? user, string anonymousKey, string? clientId = null, string? clientIp = null);
}

// Single place that decides whether an AI request is allowed - used by both AiController (the HTTP
// surface hit by the mobile/admin apps) and the Blazor Server pages that call AiService in-process.
//
// Limits, all of which must pass:
//  1. Global daily circuit breaker (OpenAI:GlobalDailyRequests) - caps total OpenAI spend per day
//     whatever else goes wrong.
//  2. Students: must hold at least one credit (callers then deduct it).
//  3. Anonymous: a per-IP daily cap (OpenAI:AnonymousIpDailyRequests, sized for a school behind one
//     NAT) AND the per-device daily allowance. The IP cap is what makes rotating the client-supplied
//     device id useless; the device allowance keeps one visitor from draining their IP's quota.
public class AiUsageGuard : IAiUsageGuard
{
    public const string GlobalKey = "global";

    private readonly IAiRateLimitService _rateLimitService;
    private readonly IAnonymousUsageService _anonymousUsage;
    private readonly IConfiguration _configuration;
    private readonly IUserService? _userService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AiUsageGuard(IAiRateLimitService rateLimitService, IAnonymousUsageService anonymousUsage, IConfiguration configuration,
        IUserService? userService = null, IHttpContextAccessor? httpContextAccessor = null)
    {
        _rateLimitService = rateLimitService;
        _anonymousUsage = anonymousUsage;
        _configuration = configuration;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int ConfigInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var v) ? v : fallback;

    public async Task<AiUsageDecision> TryConsumeAsync(ClaimsPrincipal? user, string anonymousKey, string? clientId = null, string? clientIp = null)
    {
        if (!_rateLimitService.TryConsume(GlobalKey, ConfigInt("OpenAI:GlobalDailyRequests", 5000)))
            return new AiUsageDecision(false, 0, "AI je dnes přetížená. Zkus to prosím zítra.", false);

        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Logged-in students pay with credits only; the free daily allowance is a logged-out perk
        // and must be neither shown nor consumed for them. The deduction itself is the caller's job,
        // but a student without credits is refused here, before anything is spent on OpenAI.
        if (user?.Identity?.IsAuthenticated == true && user.FindFirst("role")?.Value == "student")
        {
            if (_userService != null && int.TryParse(userId, out var studentId))
            {
                var student = await _userService.GetByIdAsync(studentId);
                if ((student?.Credits ?? 0) < 1)
                    return new AiUsageDecision(false, 0, "Nemáš dostatek kreditů.", false);
            }
            return new AiUsageDecision(true, 0, null, false);
        }

        if (user?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId))
        {
            // Renews daily. Keyed by account id (not IP), so it isn't shared across a network.
            var key = $"user:{userId}";
            var baseLimit = ConfigInt("OpenAI:MaxDailyRequests", 10);
            if (!string.IsNullOrEmpty(clientId))
                baseLimit = ConfigInt($"OpenAI:ClientLimits:{clientId}", baseLimit);

            var effectiveLimit = baseLimit + _rateLimitService.GetBonusTotal(anonymousKey);
            var allowed = _rateLimitService.TryConsume(key, effectiveLimit);
            var remaining = _rateLimitService.GetRemaining(key, effectiveLimit);
            return new AiUsageDecision(allowed, remaining,
                allowed ? null : "Denní limit dotazů byl vyčerpán.", false);
        }

        // Anonymous.
        var bonus = _rateLimitService.GetBonusTotal(anonymousKey);
        var deniedMessage = "Vyčerpal jsi dnešní limit AI dotazů zdarma. Zkus to zítra, nebo se zaregistruj a získej víc kreditů.";

        // Per-IP cap first: it can't be reset by the client, and checking it before the device
        // allowance also bounds how many AnonymousAiUsages rows one IP can create per day.
        var ip = clientIp ?? _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ipRemaining = int.MaxValue;
        if (!string.IsNullOrEmpty(ip))
        {
            var ipKey = $"anon-ip:{ip}";
            var ipLimit = ConfigInt("OpenAI:AnonymousIpDailyRequests", 200) + bonus;
            if (!_rateLimitService.TryConsume(ipKey, ipLimit))
                return new AiUsageDecision(false, 0, deniedMessage, true);
            ipRemaining = _rateLimitService.GetRemaining(ipKey, ipLimit);
        }

        var anonLimit = ConfigInt("OpenAI:AnonymousDailyRequests", 20) + bonus;
        var anonAllowed = await _anonymousUsage.TryConsumeAsync(anonymousKey, anonLimit);
        var anonRemaining = await _anonymousUsage.GetRemainingAsync(anonymousKey, anonLimit);
        return new AiUsageDecision(anonAllowed, Math.Min(anonRemaining, ipRemaining),
            anonAllowed ? null : deniedMessage, !anonAllowed);
    }
}
