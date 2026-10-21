using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tobiso.Web.App.Authentication;

public class StudentAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly StudentCredentialStore _store;
    private readonly ILogger<StudentAuthStateProvider> _logger;
    private readonly byte[] _jwtSecretBytes;
    private Timer? _expiryTimer;
    private DateTimeOffset? _scheduledExpiry;

    public StudentAuthStateProvider(StudentCredentialStore store, ILogger<StudentAuthStateProvider> logger, IConfiguration configuration)
    {
        _store = store;
        _logger = logger;
        _jwtSecretBytes = Encoding.UTF8.GetBytes(configuration["Auth:Jwt:Secret"] ?? "");
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
            _logger.LogWarning(ex, "Failed to parse student JWT - treating as unauthenticated");
            return Anonymous();
        }
    }

    public void NotifyStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    // Components such as NavMenu cache the identity and only refresh on AuthenticationStateChanged,
    // so without this an expired token leaves them showing a logged-in user until a reload.
    private void ScheduleExpiryNotification(DateTimeOffset expiry)
    {
        lock (this)
        {
            if (_scheduledExpiry == expiry) return;
            _scheduledExpiry = expiry;
            // +1s so the re-evaluation lands after exp; clamp to the Timer's ~49-day limit.
            var delay = expiry - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
            var max = TimeSpan.FromDays(40);
            if (delay > max) delay = max;
            _expiryTimer?.Dispose();
            _expiryTimer = new Timer(_ => NotifyStateChanged(), null, delay, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (this)
        {
            _expiryTimer?.Dispose();
            _expiryTimer = null;
        }
    }

    private ClaimsPrincipal ParseToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return AnonymousPrincipal();

        // Verify the HMAC signature before trusting any claim in the payload - this token
        // originates from client-controlled storage (localStorage), so without this check
        // anyone could hand-craft a payload (e.g. a different user's "sub") and this provider
        // would treat it as a valid identity for every page that reads AuthenticationState.
        var signingInput = $"{parts[0]}.{parts[1]}";
        var expectedSig = HMACSHA256.HashData(_jwtSecretBytes, Encoding.UTF8.GetBytes(signingInput));
        var actualSig = Base64UrlDecode(parts[2]);
        if (!CryptographicOperations.FixedTimeEquals(actualSig, expectedSig))
            return AnonymousPrincipal();

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("exp", out var expElem))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expElem.GetInt64());
            if (expiry.UtcDateTime < DateTime.UtcNow)
                return AnonymousPrincipal();
            ScheduleExpiryNotification(expiry);
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
                case "avatar":
                    claims.Add(new Claim("avatar", prop.Value.GetString() ?? ""));
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
