using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Tobiso.Web.App.Authentication;

public class BasicAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly CredentialStore _credentialStore;
    private readonly ILogger<BasicAuthenticationStateProvider> _logger;

    public BasicAuthenticationStateProvider(
        CredentialStore credentialStore,
        ILogger<BasicAuthenticationStateProvider> logger)
    {
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var credentials = _credentialStore.Get();
        
        if (credentials.HasValue)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, credentials.Value.Username),
                new Claim(ClaimTypes.NameIdentifier, credentials.Value.Username)
            };

            var identity = new ClaimsIdentity(claims, "basic");
            var principal = new ClaimsPrincipal(identity);
            
            _logger.LogDebug("User {Username} is authenticated", credentials.Value.Username);
            return Task.FromResult(new AuthenticationState(principal));
        }

        _logger.LogDebug("No authentication credentials found");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}