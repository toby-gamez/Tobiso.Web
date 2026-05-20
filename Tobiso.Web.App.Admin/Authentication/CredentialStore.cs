using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Stores the current user's JWT token in memory and in browser localStorage.
/// Registered as Singleton; the token in localStorage is restored per-circuit via
/// <see cref="InitializeAsync"/> called from the root App component.
///
/// NOTE: Singleton lifetime means a server restart or a second concurrent admin login
/// will overwrite the in-memory token. The per-browser localStorage copy ensures
/// each browser session restores its own token independently. This is acceptable for
/// a single-admin deployment — revisit if multi-admin support is added.
/// </summary>
public class CredentialStore
{
    private const string TokenStorageKey    = "tobiso_jwt";
    // Old keys — removed on next logout to clean up plaintext passwords
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

    /// <summary>Restores a previously stored JWT token from browser localStorage.</summary>
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

    /// <summary>Stores the JWT token in memory and persists it to localStorage.</summary>
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

    /// <summary>Sets the token in memory only (no async localStorage write).</summary>
    public void Set(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged();
    }

    /// <summary>Returns the current JWT token, or null if not authenticated.</summary>
    public string? GetToken() => _token;

    /// <summary>Clears the token from memory and localStorage, and removes legacy Basic-auth keys.</summary>
    public async Task ClearAsync(IJSRuntime jsRuntime)
    {
        _token = null;
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
            // Remove legacy plaintext keys if they still exist
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

    /// <summary>Clears the token from memory only (no async localStorage write).</summary>
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
