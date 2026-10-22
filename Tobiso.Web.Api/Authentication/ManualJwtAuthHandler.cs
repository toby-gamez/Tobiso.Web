using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tobiso.Api.Authentication;

public class ManualJwtAuthHandler : AuthenticationHandler<ManualJwtAuthOptions>
{
    public ManualJwtAuthHandler(
        IOptionsMonitor<ManualJwtAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadToken(out var fromCookie);
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        ClaimsPrincipal principal;
        try
        {
            principal = ValidateToken(token, Options);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "JWT validation failed");
            return AuthenticateResult.Fail("JWT validation failed");
        }

        if (Options.ValidatePrincipalAsync != null)
        {
            bool valid;
            try
            {
                valid = await Options.ValidatePrincipalAsync(principal, Context);
            }
            catch (Exception ex)
            {
                // E.g. the database is briefly unreachable: don't authenticate this request, but keep
                // the cookie - the session itself may be perfectly fine.
                Logger.LogError(ex, "Session validation failed");
                return AuthenticateResult.Fail("Session could not be validated");
            }

            if (!valid)
            {
                // Stale session (revoked or deleted account): drop the cookie so the browser stops sending it.
                if (fromCookie) SessionCookies.Delete(Response, Options.CookieName!);
                return AuthenticateResult.Fail("Session is no longer valid");
            }
        }

        if (Options.IncludeRawTokenClaim && principal.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(RawTokenClaim, token));

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    /// <summary>Claim carrying the raw token, for server-side code that calls another API on the user's behalf.</summary>
    public const string RawTokenClaim = "access_token";

    // The Authorization header wins; the HttpOnly cookie is the browser session.
    private string? ReadToken(out bool fromCookie)
    {
        fromCookie = false;
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : null;
        }

        if (string.IsNullOrEmpty(Options.CookieName) || !IsCookieUseAllowed(Request))
            return null;

        var cookie = Request.Cookies[Options.CookieName];
        fromCookie = !string.IsNullOrEmpty(cookie);
        return cookie;
    }

    // SameSite=Lax already keeps the cookie off cross-site POSTs; this also refuses it on any
    // cross-site or same-site (e.g. a subdomain) request, except a plain top-level page load, so
    // another site can't spend a student's credits or act as them through a link or form.
    // Sec-Fetch-Site is set by every current browser; requests without it aren't from a browser.
    private static bool IsCookieUseAllowed(HttpRequest request)
    {
        var site = request.Headers["Sec-Fetch-Site"].ToString();
        if (string.IsNullOrEmpty(site) || site == "same-origin" || site == "none")
            return true;

        return HttpMethods.IsGet(request.Method)
            && request.Headers["Sec-Fetch-Mode"].ToString() == "navigate"
            && !request.Path.StartsWithSegments("/api");
    }

    // Public so the admin Blazor app can verify the token it restores from localStorage with the
    // exact same rules the API applies, instead of trusting an unsigned payload.
    public static ClaimsPrincipal ValidateToken(string token, ManualJwtAuthOptions options)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            throw new InvalidOperationException("Invalid JWT format");

        var headerJson  = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var signature   = Base64UrlDecode(parts[2]);

        var signingInput = $"{parts[0]}.{parts[1]}";
        var keyBytes     = Encoding.UTF8.GetBytes(options.Secret);
        var expectedSig  = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(signingInput));

        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSig))
            throw new InvalidOperationException("JWT signature is invalid");

        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        // A missing iss/aud/exp fails validation rather than skipping the check.
        if (options.ValidateIssuer &&
            (!root.TryGetProperty("iss", out var issElem) || issElem.GetString() != options.ValidIssuer))
            throw new InvalidOperationException("Invalid or missing issuer");

        if (options.ValidateAudience &&
            (!root.TryGetProperty("aud", out var audElem) || audElem.GetString() != options.ValidAudience))
            throw new InvalidOperationException("Invalid or missing audience");

        if (options.ValidateLifetime)
        {
            if (!root.TryGetProperty("exp", out var expElem) || expElem.ValueKind != JsonValueKind.Number)
                throw new InvalidOperationException("JWT has no expiry");
            if (DateTimeOffset.FromUnixTimeSeconds(expElem.GetInt64()).UtcDateTime < DateTime.UtcNow)
                throw new InvalidOperationException("JWT token has expired");
        }

        var claims = new List<Claim>();
        foreach (var prop in root.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "sub":
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, prop.Value.GetString() ?? ""));
                    break;
                case "unique_name":
                case "name":
                    claims.Add(new Claim(ClaimTypes.Name, prop.Value.GetString() ?? ""));
                    break;
                case "nameidentifier":
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, prop.Value.GetString() ?? ""));
                    break;
                default:
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        claims.Add(new Claim(prop.Name, prop.Value.GetString() ?? ""));
                    else if (prop.Value.ValueKind == JsonValueKind.Number)
                        claims.Add(new Claim(prop.Name, prop.Value.GetRawText()));
                    break;
            }
        }

        // "role" is the role claim type, so IsInRole("student") works on these principals.
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, "role"));
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var len = input.Length;
        var padded = (len % 4) switch
        {
            2 => input + "==",
            3 => input + "=",
            _ => input
        };
        return Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
    }
}

public class ManualJwtAuthOptions : AuthenticationSchemeOptions
{
    public string Secret { get; set; } = "";
    public bool ValidateIssuer { get; set; } = true;
    public string? ValidIssuer { get; set; }
    public bool ValidateAudience { get; set; } = true;
    public string? ValidAudience { get; set; }
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>When set, a browser session can carry the token in this HttpOnly cookie instead of a header.</summary>
    public string? CookieName { get; set; }

    /// <summary>Adds the raw token as a <see cref="ManualJwtAuthHandler.RawTokenClaim"/> claim.</summary>
    public bool IncludeRawTokenClaim { get; set; }

    /// <summary>Extra check after the signature and lifetime pass, e.g. that the session wasn't revoked.</summary>
    public Func<ClaimsPrincipal, HttpContext, Task<bool>>? ValidatePrincipalAsync { get; set; }
}
