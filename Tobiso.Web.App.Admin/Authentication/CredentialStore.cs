
namespace Tobiso.Web.App.Authentication;

/// <summary>
/// Holds the admin's JWT for server-side calls to the main app's API, one instance per Blazor circuit
/// (Scoped). It is filled from the authenticated user (the token arrives in an HttpOnly cookie and is
/// never handed to page script). It must never be shared between circuits: an earlier version kept a
/// static fallback copy of the last token, so any visitor's circuit could fall back to the last admin's.
/// AuthenticationHeaderHandler is resolved outside the circuit's DI scope, so it reaches the
/// circuit's store through <see cref="Current"/>, set by <see cref="CredentialCircuitHandler"/>
/// for every inbound circuit activity.
/// </summary>
public class CredentialStore
{
    private static readonly AsyncLocal<CredentialStore?> _current = new();
    private string? _token;

    /// <summary>The store of the circuit whose activity is currently running (null outside a circuit).</summary>
    internal static CredentialStore? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public void Set(string? token) => _token = token;

    /// <summary>Returns the current JWT token, or null if not authenticated.</summary>
    public string? GetToken() => _token;

    public void Clear() => _token = null;
}
