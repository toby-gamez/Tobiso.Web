using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Stores the current admin's JWT token, one instance per Blazor circuit (Scoped).
/// It must never be shared between circuits: an earlier version kept a static fallback copy of
/// the last token, so any visitor's circuit could fall back to the last admin's token.
/// AuthenticationHeaderHandler is resolved outside the circuit's DI scope, so it reaches the
/// circuit's store through <see cref="Current"/>, set by <see cref="CredentialCircuitHandler"/>
/// for every inbound circuit activity.
/// </summary>
public class CredentialStore
{
    private const string TokenStorageKey    = "tobiso_jwt";
    // Old keys - removed on next logout to clean up plaintext passwords
    private const string LegacyUsernameKey = "blinked_username";
    private const string LegacyPasswordKey  = "blinked_password";

    private static readonly AsyncLocal<CredentialStore?> _current = new();
    private string? _token;
    private readonly ILogger<CredentialStore> _logger;

    /// <summary>The store of the circuit whose activity is currently running (null outside a circuit).</summary>
    internal static CredentialStore? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

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
                _token = token;
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store JWT token in localStorage");
        }
    }

    /// <summary>Sets the token in memory only (no async localStorage write).</summary>
    public void Set(string token)
    {
        _token = token;
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
            // Token cleared from storage. Avoid logging token contents or metadata.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear JWT token from localStorage");
        }
    }

    /// <summary>Clears the token from memory only (no async localStorage write).</summary>
    public void Clear()
    {
        _token = null;
    }
}
