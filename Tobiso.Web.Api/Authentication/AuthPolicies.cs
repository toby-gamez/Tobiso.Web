using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Tobiso.Api.Authentication;

/// <summary>
/// Student and admin JWTs are signed with the same secret, issuer and audience - they differ only
/// by their <c>role</c> claim. A bare <c>[Authorize]</c> therefore has to mean "admin", or any
/// registered student could call the content-management endpoints with their own token.
/// Admin is an allow-list (<c>role=admin</c> required), not "anything that isn't a student", so a
/// token path that ever forgets its role claim fails closed instead of minting an admin.
/// </summary>
public static class AuthPolicies
{
    /// <summary>Any signed-in caller, students included. Opt in explicitly on student-facing endpoints.</summary>
    public const string AnyUser = "AnyUser";

    public const string AdminRole = "admin";

    public static bool IsStudent(ClaimsPrincipal user) => user.FindFirst("role")?.Value == "student";

    public static bool IsAdmin(ClaimsPrincipal user) => user.FindFirst("role")?.Value == AdminRole;

    /// <summary>
    /// Makes the default policy (bare <c>[Authorize]</c>) admin-only and registers <see cref="AnyUser"/>.
    /// </summary>
    public static IServiceCollection AddTobisoAuthorization(this IServiceCollection services) =>
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx => IsAdmin(ctx.User))
                .Build();

            options.AddPolicy(AnyUser, p => p.RequireAuthenticatedUser());
        });
}
