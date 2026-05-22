using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Tobiso.Web.App.Authentication;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CredentialStore _credentialStore;
    private readonly ILogger<TokenAuthenticationStateProvider> _logger;

    public TokenAuthenticationStateProvider(
        CredentialStore credentialStore,
        ILogger<TokenAuthenticationStateProvider> logger)
    {
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _credentialStore.GetToken();
        if (string.IsNullOrEmpty(token))
            return Anonymous();

        try
        {
            var principal = ParseToken(token);
            return Task.FromResult(new AuthenticationState(principal));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT token — treating as unauthenticated");
            return Anonymous();
        }
    }

    public void NotifyAuthenticationStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static ClaimsPrincipal ParseToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return AnonymousPrincipal();

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        // Check expiration
        if (root.TryGetProperty("exp", out var expElem))
        {
            var expUnix = expElem.GetInt64();
            if (DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime < DateTime.UtcNow)
                return AnonymousPrincipal();
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
            }
        }

        claims.Add(new Claim("jwt", token));

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal AnonymousPrincipal()
        => new(new ClaimsIdentity());

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

    private static Task<AuthenticationState> Anonymous()
        => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}