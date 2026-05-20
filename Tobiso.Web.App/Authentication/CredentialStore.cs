using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

public class CredentialStore
{
    private const string TokenStorageKey = "tobiso_jwt";
    private const string LegacyUsernameKey = "blinked_username";
    private const string LegacyPasswordKey  = "blinked_password";

    private string? _token;
    private readonly ILogger<CredentialStore> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CredentialStore(ILogger<CredentialStore> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
                _logger.LogDebug("Restored JWT token from localStorage");
                NotifyAuthenticationStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore JWT token from localStorage");
        }
    }

    public async Task SetAsync(string token, IJSRuntime jsRuntime)
    {
        _token = token;
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, token);
            _logger.LogDebug("Stored JWT token in localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store JWT token in localStorage");
        }
        NotifyAuthenticationStateChanged();
    }

    public void Set(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged();
    }

    public string? GetToken() => _token;

    public async Task ClearAsync(IJSRuntime jsRuntime)
    {
        _token = null;
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyUsernameKey);
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LegacyPasswordKey);
            _logger.LogDebug("Cleared JWT token from localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear JWT token from localStorage");
        }
        NotifyAuthenticationStateChanged();
    }

    public void Clear()
    {
        _token = null;
        NotifyAuthenticationStateChanged();
    }

    private void NotifyAuthenticationStateChanged()
    {
        try
        {
            var authStateProvider = _serviceProvider.GetService<AuthenticationStateProvider>()
                as TokenAuthenticationStateProvider;
            authStateProvider?.NotifyAuthenticationStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify authentication state changed");
        }
    }
}