using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Stores the current user's JWT token.
/// Uses AsyncLocal internally so each Blazor circuit sees its own token.
/// Registered as Singleton so AuthenticationHeaderHandler (resolved from root scope)
/// can inject it directly without scope-mismatch issues.
/// </summary>
public class CredentialStore
{
    private const string TokenStorageKey    = "tobiso_jwt";
    // Old keys — removed on next logout to clean up plaintext passwords
    private const string LegacyUsernameKey = "blinked_username";
    private const string LegacyPasswordKey  = "blinked_password";

    private static readonly AsyncLocal<string?> _asyncToken = new();
    private static string? _directToken;
    private readonly ILogger<CredentialStore> _logger;

    /// <summary>Direct static access for AuthenticationHeaderHandler (bypasses AsyncLocal/ExecutionContext).</summary>
    internal static string? DirectToken => _directToken;

    public CredentialStore(ILogger<CredentialStore> logger)
    {
        _logger = logger;
    }

    /// <summary>Restores a previously stored JWT token from browser localStorage.</summary>
    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
            if (!string.IsNullOrEmpty(token))
            {
                _asyncToken.Value = token;
                _directToken = token;
                _logger.LogDebug("Restored JWT token from localStorage");
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
        _asyncToken.Value = token;
        _directToken = token;
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, token);
            _logger.LogDebug("Stored JWT token in localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store JWT token in localStorage");
        }
    }

    /// <summary>Sets the token in memory only (no async localStorage write).</summary>
    public void Set(string token)
    {
        _asyncToken.Value = token;
        _directToken = token;
    }

    /// <summary>Returns the current JWT token, or null if not authenticated.</summary>
    public string? GetToken() => _asyncToken.Value ?? _directToken;

    /// <summary>Static accessor for AuthenticationHeaderHandler — reads the per-circuit token without DI.</summary>
    public static string? CurrentToken => _asyncToken.Value;

    /// <summary>Clears the token from memory and localStorage, and removes legacy Basic-auth keys.</summary>
    public async Task ClearAsync(IJSRuntime jsRuntime)
    {
        _asyncToken.Value = null;
        _directToken = null;
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
    }

    /// <summary>Clears the token from memory only (no async localStorage write).</summary>
    public void Clear()
    {
        _asyncToken.Value = null;
        _directToken = null;
    }
}
