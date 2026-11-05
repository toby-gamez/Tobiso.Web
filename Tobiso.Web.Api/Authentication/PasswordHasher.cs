using System.Security.Cryptography;
using System.Text;

namespace Tobiso.Api.Authentication;

public static class PasswordHasher
{
    // OWASP's current recommendation for PBKDF2-HMAC-SHA256. Hashes made with fewer iterations still
    // verify (the count is stored in the hash) and are upgraded on the next successful login.
    public const int Iterations = 600_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>True when the stored hash uses fewer iterations than <see cref="Iterations"/> (or is not PBKDF2).</summary>
    public static bool NeedsRehash(string stored)
    {
        var parts = stored.Split(':');
        return parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var it) || it < Iterations;
    }

    // A fixed hash for a password nobody has, used to spend the same time on a login for an unknown
    // email as for a known one, so response timing doesn't reveal which emails are registered.
    private static readonly Lazy<string> DummyHash = new(() => Hash(Guid.NewGuid().ToString()));

    public static void VerifyDummy(string password) => Verify(password, DummyHash.Value);

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 4 || parts[0] != "PBKDF2") return false;
        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
