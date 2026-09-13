using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

// Registered Scoped (one instance per Blazor circuit, i.e. per connected user) - this must
// never be Singleton: it holds a live per-user JWT, and a Singleton's fields are shared by
// every circuit on the process, which would leak one user's token to another's session.
public class StudentCredentialStore
{
    private const string TokenKey = "tobiso_student_token";

    private string? _token;

    private readonly ILogger<StudentCredentialStore> _logger;

    public StudentCredentialStore(ILogger<StudentCredentialStore> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(IJSRuntime js)
    {
        try
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                _token = token;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore student token from localStorage");
        }
    }

    public async Task SetAsync(string token, IJSRuntime js)
    {
        _token = token;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store student token in localStorage");
        }
    }

    public void Set(string token)
    {
        _token = token;
    }

    public string? GetToken() => _token;

    public async Task ClearAsync(IJSRuntime js)
    {
        _token = null;
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear student token from localStorage");
        }
    }

    public void Clear()
    {
        _token = null;
    }
}
