using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

public class CredentialStore
{
    private (string Username, string Password)? _credentials;
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
            var username = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "blinked_username");
            var password = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", "blinked_password");
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _credentials = (username, password);
                _logger.LogDebug("Restored credentials from localStorage for user {Username}", username);
                
                // Notify authentication state provider
                NotifyAuthenticationStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore credentials from localStorage");
        }
    }

    public async Task SetAsync(string username, string password, IJSRuntime jsRuntime)
    {
        _credentials = (username, password);
        
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "blinked_username", username);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "blinked_password", password);
            _logger.LogDebug("Stored credentials in localStorage for user {Username}", username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store credentials in localStorage");
        }
        
        NotifyAuthenticationStateChanged();
    }

    public void Set(string username, string password)
    {
        _credentials = (username, password);
        NotifyAuthenticationStateChanged();
    }

    public (string Username, string Password)? Get()
    {
        return _credentials;
    }

    public async Task ClearAsync(IJSRuntime jsRuntime)
    {
        _credentials = null;
        
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "blinked_username");
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "blinked_password");
            _logger.LogDebug("Cleared credentials from localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear credentials from localStorage");
        }
        
        NotifyAuthenticationStateChanged();
    }

    public void Clear()
    {
        _credentials = null;
        NotifyAuthenticationStateChanged();
    }

    private void NotifyAuthenticationStateChanged()
    {
        try
        {
            var authStateProvider = _serviceProvider.GetService<AuthenticationStateProvider>() as BasicAuthenticationStateProvider;
            authStateProvider?.NotifyAuthenticationStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify authentication state changed");
        }
    }
}