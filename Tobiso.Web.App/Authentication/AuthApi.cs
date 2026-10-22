using Microsoft.JSInterop;

namespace Tobiso.Web.App.Authentication;

public record AuthApiResult(int Status, string? Message, string? Code)
{
    public bool Ok => Status is >= 200 and < 300;
}

/// <summary>Calls /api/auth/* from the browser (wwwroot/js/auth.js) so the session cookie lands there.</summary>
public static class AuthApi
{
    public static async Task<AuthApiResult> PostAsync(IJSRuntime js, string url, object? body = null) =>
        await js.InvokeAsync<AuthApiResult>("tobisoAuthPost", url, body ?? new { });

    /// <summary>A user-facing message for a failed call, preferring the server's own.</summary>
    public static string ErrorText(AuthApiResult result, string fallback) => result.Status switch
    {
        429 => "Příliš mnoho pokusů. Zkus to prosím za chvíli znovu.",
        0 => "Nepodařilo se spojit se serverem. Zkus to prosím znovu.",
        _ => result.Message ?? fallback
    };
}
