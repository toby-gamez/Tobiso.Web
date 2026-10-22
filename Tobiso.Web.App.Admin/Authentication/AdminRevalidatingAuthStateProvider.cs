using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Tobiso.Api.Authentication;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// The circuit's user is whoever the HttpOnly admin session cookie authenticated. Re-checked every
/// minute so an open tab stops being admin as soon as the token expires.
/// </summary>
public class AdminRevalidatingAuthStateProvider(ILoggerFactory loggerFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var user = authenticationState.User;
        var valid = AuthPolicies.IsAdmin(user)
            && SessionCookies.GetExpiry(user) is { } expiry
            && expiry > DateTimeOffset.UtcNow;
        return Task.FromResult(valid);
    }
}
