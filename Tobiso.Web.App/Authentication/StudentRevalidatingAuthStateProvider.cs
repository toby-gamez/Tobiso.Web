using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// The circuit starts with the user the session cookie authenticated (see ManualJwtAuthHandler).
/// A circuit can stay open for hours, so this re-checks that identity every minute: once the token
/// expires or its session is revoked (password reset, "log out everywhere", account deleted), the
/// open tab drops to anonymous instead of carrying on as the user.
/// </summary>
public class StudentRevalidatingAuthStateProvider(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var user = authenticationState.User;
        if (user.Identity?.IsAuthenticated != true) return true;

        if (SessionCookies.GetExpiry(user) is not { } expiry || expiry < DateTimeOffset.UtcNow)
            return false;

        await using var scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        return await SessionCookies.IsStudentSessionValidAsync(user, users);
    }
}
