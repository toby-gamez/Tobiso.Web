using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

public class StudentCredentialStore
{
    private const string TokenKey = "tobiso_student_token";

    private static readonly AsyncLocal<string?> _asyncToken = new();
    private static string? _directToken;

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
                _asyncToken.Value = token;
                _directToken = token;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore student token from localStorage");
        }
    }

    public async Task SetAsync(string token, IJSRuntime js)
    {
        _asyncToken.Value = token;
        _directToken = token;
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
        _asyncToken.Value = token;
        _directToken = token;
    }

    public string? GetToken() => _asyncToken.Value ?? _directToken;

    public async Task ClearAsync(IJSRuntime js)
    {
        _asyncToken.Value = null;
        _directToken = null;
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
        _asyncToken.Value = null;
        _directToken = null;
    }
}
