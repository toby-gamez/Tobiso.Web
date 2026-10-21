using Microsoft.AspNetCore.Authentication;
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

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var principal = ValidateToken(token, Options);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "JWT validation failed");
            return Task.FromResult(AuthenticateResult.Fail("JWT validation failed: " + ex.Message));
        }
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

        if (options.ValidateIssuer && root.TryGetProperty("iss", out var issElem))
        {
            if (issElem.GetString() != options.ValidIssuer)
                throw new InvalidOperationException($"Invalid issuer: {issElem.GetString()}");
        }

        if (options.ValidateAudience && root.TryGetProperty("aud", out var audElem))
        {
            if (audElem.GetString() != options.ValidAudience)
                throw new InvalidOperationException($"Invalid audience: {audElem.GetString()}");
        }

        if (options.ValidateLifetime && root.TryGetProperty("exp", out var expElem))
        {
            var expUnix = expElem.GetInt64();
            if (DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime < DateTime.UtcNow)
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

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
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
}
