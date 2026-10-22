using System.Collections.Concurrent;

namespace Tobiso.Api.Authentication;

/// <summary>
/// Per-IP lockout for the shared admin credential. Basic auth is accepted on every admin endpoint
/// (not only the rate-limited AuthController), so without this the admin password could be guessed
/// without limit - and every guess costs a full PBKDF2 run, which also makes it a cheap CPU DoS.
/// Once an IP is locked out, attempts are rejected before the password is hashed.
/// In-memory and per-process, which is enough for a single IIS instance.
/// </summary>
public static class AdminLoginThrottle
{
    private const int MaxFailures = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _failures = new();

    public static bool IsLockedOut(string? ip)
    {
        if (!_failures.TryGetValue(Key(ip), out var entry)) return false;
        if (DateTime.UtcNow - entry.WindowStart > Window)
        {
            _failures.TryRemove(Key(ip), out _);
            return false;
        }
        return entry.Count >= MaxFailures;
    }

    public static void RecordFailure(string? ip)
    {
        var now = DateTime.UtcNow;
        _failures.AddOrUpdate(Key(ip),
            _ => (1, now),
            (_, e) => now - e.WindowStart > Window ? (1, now) : (e.Count + 1, e.WindowStart));

        // Keep memory bounded if many distinct IPs fail.
        if (_failures.Count > 10_000)
        {
            foreach (var kv in _failures)
                if (now - kv.Value.WindowStart > Window)
                    _failures.TryRemove(kv.Key, out _);
        }
    }

    public static void RecordSuccess(string? ip) => _failures.TryRemove(Key(ip), out _);

    private static string Key(string? ip) => string.IsNullOrEmpty(ip) ? "unknown" : ip;
}
