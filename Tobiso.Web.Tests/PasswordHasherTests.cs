using Tobiso.Api.Authentication;

namespace Tobiso.Web.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void NewHash_UsesCurrentIterations_AndVerifies()
    {
        var hash = PasswordHasher.Hash("correct horse battery");
        Assert.StartsWith($"PBKDF2:{PasswordHasher.Iterations}:", hash);
        Assert.True(PasswordHasher.Verify("correct horse battery", hash));
        Assert.False(PasswordHasher.Verify("wrong", hash));
        Assert.False(PasswordHasher.NeedsRehash(hash));
    }

    [Fact]
    public void LegacyHash_StillVerifies_AndNeedsRehash()
    {
        // A 100k-iteration hash in the old format.
        var salt = new byte[16];
        var dk = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("pw"u8.ToArray(), salt, 100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        var legacy = $"PBKDF2:100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(dk)}";
        Assert.True(PasswordHasher.Verify("pw", legacy));
        Assert.True(PasswordHasher.NeedsRehash(legacy));
    }
}
