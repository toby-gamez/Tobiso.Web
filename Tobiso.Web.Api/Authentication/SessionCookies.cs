using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tobiso.Web.Api.Services;

namespace Tobiso.Api.Authentication;

/// <summary>
/// Browser sessions keep their JWT in an HttpOnly cookie, so page script (and therefore any XSS)
/// can never read it. Host-only (no Domain), so the student site and the admin panel don't share it.
/// </summary>
public static class SessionCookies
{
    public const string Student = "tobiso_auth";
    public const string Admin = "tobiso_admin";

    /// <summary>Claim holding the user's security stamp at the time the token was issued.</summary>
    public const string SecurityStampClaim = "sst";

    public static void Append(HttpResponse response, string name, string token, DateTimeOffset expires) =>
        response.Cookies.Append(name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires,
            IsEssential = true
        });

    public static void Delete(HttpResponse response, string name) =>
        response.Cookies.Delete(name, new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.Lax });

    /// <summary>
    /// A student session stays valid only while the account exists and its security stamp is the one
    /// the token was issued with - rotating the stamp (password reset, "log out everywhere", ...) ends
    /// every session at once. Non-student principals aren't tied to a user row and pass.
    /// </summary>
    public static async Task<bool> IsStudentSessionValidAsync(ClaimsPrincipal principal, IUserService users)
    {
        if (!AuthPolicies.IsStudent(principal)) return true;
        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return false;
        var stamp = principal.FindFirst(SecurityStampClaim)?.Value;
        return !string.IsNullOrEmpty(stamp) && await users.IsSessionValidAsync(userId, stamp);
    }

    public static DateTimeOffset? GetExpiry(ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirst("exp")?.Value, out var exp) ? DateTimeOffset.FromUnixTimeSeconds(exp) : null;
}
