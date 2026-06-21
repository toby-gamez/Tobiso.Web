using System.Net.Http.Headers;

namespace Tobiso.Web.App.Authentication;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly ILogger<AuthenticationHeaderHandler> _logger;

    public AuthenticationHeaderHandler(ILogger<AuthenticationHeaderHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = CredentialStore.CurrentToken ?? CredentialStore.DirectToken;
        var hasToken = !string.IsNullOrEmpty(token);
        // Log only whether a token is present; avoid logging token length or value.
        _logger.LogInformation("[AuthHandler] Request to {Url}, token present: {HasToken}",
            request.RequestUri?.PathAndQuery, hasToken);
        if (hasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("[AuthHandler] No JWT token present — request will be unauthenticated");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
