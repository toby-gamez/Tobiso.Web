using System.Net.Http.Headers;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Injects the JWT Bearer token from <see cref="CredentialStore"/> into every outgoing Refit request.
/// </summary>
public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly CredentialStore _credentialStore;
    private readonly ILogger<AuthenticationHeaderHandler> _logger;

    public AuthenticationHeaderHandler(CredentialStore credentialStore, ILogger<AuthenticationHeaderHandler> logger)
    {
        _credentialStore = credentialStore;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _credentialStore.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("[AuthHandler] Injected Bearer token");
        }
        else
        {
            _logger.LogWarning("[AuthHandler] No JWT token present — request will be unauthenticated");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
