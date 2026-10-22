using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Tobiso.Api.Authentication;

namespace Tobiso.Web.App.Authentication;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CredentialStore _credentialStore;
    private readonly ILogger<TokenAuthenticationStateProvider> _logger;
    private readonly IOptionsMonitor<ManualJwtAuthOptions> _jwtOptions;

    public TokenAuthenticationStateProvider(
        CredentialStore credentialStore,
        ILogger<TokenAuthenticationStateProvider> logger,
        IOptionsMonitor<ManualJwtAuthOptions> jwtOptions)
    {
        _credentialStore = credentialStore;
        _logger = logger;
        _jwtOptions = jwtOptions;
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
            _logger.LogWarning(ex, "Failed to parse JWT token - treating as unauthenticated");
            return Anonymous();
        }
    }

    public void NotifyAuthenticationStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    // Verifies the signature (same rules as the API's ManualJwtAuthHandler) - the token comes
    // from localStorage, so decoding the payload alone would let anyone hand-craft one. Student
    // tokens are valid signatures too, so only an explicit role=admin is let in.
    private ClaimsPrincipal ParseToken(string token)
    {
        var principal = ManualJwtAuthHandler.ValidateToken(token, _jwtOptions.Get("Bearer"));
        return AuthPolicies.IsAdmin(principal) ? principal : AnonymousPrincipal();
    }

    private static ClaimsPrincipal AnonymousPrincipal()
        => new(new ClaimsIdentity());

    private static Task<AuthenticationState> Anonymous()
        => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}
