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
        var token = CredentialStore.DirectToken;
        var hasToken = !string.IsNullOrEmpty(token);
        _logger.LogInformation("[AuthHandler] Request to {Url}, token found: {HasToken}, len: {Len}",
            request.RequestUri?.PathAndQuery, hasToken, token?.Length ?? 0);
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
