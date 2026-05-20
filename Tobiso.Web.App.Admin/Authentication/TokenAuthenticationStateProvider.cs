using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Tobiso.Web.App.Authentication;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CredentialStore _credentialStore;
    private readonly ILogger<TokenAuthenticationStateProvider> _logger;
    private static readonly JwtSecurityTokenHandler _tokenHandler = new();

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

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                if (_tokenHandler.CanReadToken(token))
                {
                    var jwt = _tokenHandler.ReadJwtToken(token);

                    if (jwt.ValidTo < DateTime.UtcNow)
                    {
                        _logger.LogDebug("JWT token has expired at {Expiry:u}", jwt.ValidTo);
                        return Anonymous();
                    }

                    var identity  = new ClaimsIdentity(jwt.Claims, "jwt");
                    var principal = new ClaimsPrincipal(identity);
                    _logger.LogDebug("User authenticated via JWT, expires {Expiry:u}", jwt.ValidTo);
                    return Task.FromResult(new AuthenticationState(principal));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT token — treating as unauthenticated");
            }
        }

        _logger.LogDebug("No valid JWT token found — unauthenticated");
        return Anonymous();
    }

    public void NotifyAuthenticationStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static Task<AuthenticationState> Anonymous()
        => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}