using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Tobiso.Web.App.Authentication;

public class StudentAuthStateProvider : AuthenticationStateProvider
{
    private readonly StudentCredentialStore _store;
    private readonly ILogger<StudentAuthStateProvider> _logger;

    public StudentAuthStateProvider(StudentCredentialStore store, ILogger<StudentAuthStateProvider> logger)
    {
        _store = store;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _store.GetToken();
        if (string.IsNullOrEmpty(token))
            return Anonymous();

        try
        {
            var principal = ParseToken(token);
            return Task.FromResult(new AuthenticationState(principal));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse student JWT — treating as unauthenticated");
            return Anonymous();
        }
    }

    public void NotifyStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static ClaimsPrincipal ParseToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return AnonymousPrincipal();

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("exp", out var expElem))
        {
            if (DateTimeOffset.FromUnixTimeSeconds(expElem.GetInt64()).UtcDateTime < DateTime.UtcNow)
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
                case "email":
                    claims.Add(new Claim(ClaimTypes.Email, prop.Value.GetString() ?? ""));
                    break;
                case "role":
                    claims.Add(new Claim(ClaimTypes.Role, prop.Value.GetString() ?? ""));
                    break;
                case "credits":
                    claims.Add(new Claim("credits", prop.Value.GetString() ?? "0"));
                    break;
            }
        }

        var identity = new ClaimsIdentity(claims, "student-jwt");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal AnonymousPrincipal() => new(new ClaimsIdentity());

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = (input.Length % 4) switch
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
