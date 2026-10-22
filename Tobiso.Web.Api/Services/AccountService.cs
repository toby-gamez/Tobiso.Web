using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public enum AccountEmailResult { Sent, Failed }

/// <summary>
/// Email verification and password reset. Links carry a stateless signed token bound to the user's
/// security stamp, so a reset link stops working the moment it's used (the reset rotates the stamp).
/// </summary>
public interface IAccountService
{
    public const int MinPasswordLength = 10;
    public const int MaxPasswordLength = 256;

    /// <summary>
    /// Creates an unverified account and mails a verification link. If the email is already taken the
    /// owner gets a password-reset link instead - same result either way, so the response doesn't
    /// reveal which emails have accounts.
    /// </summary>
    Task<AccountEmailResult> RegisterAsync(string email, string displayName, string password);
    Task<AccountEmailResult> SendVerificationAsync(AppUser user);
    /// <summary>Mails a reset link if an account exists; callers report success either way.</summary>
    Task<AccountEmailResult> RequestPasswordResetAsync(string email);
    /// <summary>Sets the new password, confirms the email and ends all other sessions. Null if the link is invalid or expired.</summary>
    Task<AppUser?> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ConfirmEmailAsync(string token);
}

public class AccountService : IAccountService
{
    private const string VerifyPurpose = "verify-email";
    private const string ResetPurpose = "reset-password";
    private static readonly TimeSpan VerifyLifetime = TimeSpan.FromHours(48);
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);
    // At most one mail of each kind per account in this window, so the endpoints can't flood an inbox.
    private static readonly TimeSpan MailCooldown = TimeSpan.FromMinutes(2);

    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;
    private readonly IUserService _users;
    private readonly IEmailSender _email;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountService> _logger;
    private readonly byte[] _key;

    public AccountService(IDbContextFactory<TobisoDbContext> dbFactory, IUserService users, IEmailSender email,
        IMemoryCache cache, IConfiguration config, ILogger<AccountService> logger)
    {
        _dbFactory = dbFactory;
        _users = users;
        _email = email;
        _cache = cache;
        _config = config;
        _logger = logger;
        // Derived from the JWT secret but domain-separated, so these can never pass as a JWT signature.
        _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(config["Auth:Jwt:Secret"] ?? ""),
            Encoding.UTF8.GetBytes("tobiso-account-links-v1"));
    }

    public async Task<AccountEmailResult> RegisterAsync(string email, string displayName, string password)
    {
        var user = await _users.RegisterAsync(email.Trim(), displayName, password, acceptedTerms: true);
        if (user != null)
            return await SendVerificationAsync(user);

        var existing = await _users.GetByEmailAsync(email);
        return existing == null
            ? AccountEmailResult.Failed
            : await SendResetAsync(existing, existingAccountOnRegister: true);
    }

    public Task<AccountEmailResult> SendVerificationAsync(AppUser user)
    {
        var link = BuildLink("overeni-emailu", CreateToken(user, VerifyPurpose, VerifyLifetime));
        return SendThrottledAsync(user, VerifyPurpose, "Potvrď svůj e-mail - Tobiso",
            $"Ahoj {user.DisplayName},\n\n" +
            "pro dokončení registrace na Tobiso potvrď svůj e-mail na tomto odkazu:\n" +
            $"{link}\n\n" +
            "Odkaz platí 48 hodin. Pokud jsi se na Tobiso neregistroval/a, tento e-mail ignoruj.\n\n" +
            "Tým Tobiso");
    }

    public async Task<AccountEmailResult> RequestPasswordResetAsync(string email)
    {
        var user = await _users.GetByEmailAsync(email);
        return user == null ? AccountEmailResult.Sent : await SendResetAsync(user, existingAccountOnRegister: false);
    }

    public async Task<AppUser?> ResetPasswordAsync(string token, string newPassword)
    {
        if (newPassword.Length is < IAccountService.MinPasswordLength or > IAccountService.MaxPasswordLength)
            return null;

        await using var db = _dbFactory.CreateDbContext();
        var user = await FindByTokenAsync(db, token, ResetPurpose);
        if (user == null) return null;

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.EmailConfirmedAt ??= DateTime.UtcNow;
        user.SecurityStamp = AppUser.NewSecurityStamp();
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ConfirmEmailAsync(string token)
    {
        await using var db = _dbFactory.CreateDbContext();
        var user = await FindByTokenAsync(db, token, VerifyPurpose);
        if (user == null) return false;

        user.EmailConfirmedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private Task<AccountEmailResult> SendResetAsync(AppUser user, bool existingAccountOnRegister)
    {
        var link = BuildLink("obnova-hesla", CreateToken(user, ResetPurpose, ResetLifetime));
        var intro = existingAccountOnRegister
            ? "někdo se právě pokusil zaregistrovat s tímto e-mailem, ale účet už máš. Pokud jsi zapomněl/a heslo, nové si nastavíš tady:"
            : "dostali jsme žádost o obnovu hesla k tvému účtu. Nové heslo si nastavíš tady:";
        return SendThrottledAsync(user, ResetPurpose, "Obnova hesla - Tobiso",
            $"Ahoj {user.DisplayName},\n\n{intro}\n{link}\n\n" +
            "Odkaz platí 1 hodinu a dá se použít jen jednou. Pokud jsi o nic nežádal/a, tento e-mail ignoruj - heslo zůstane beze změny.\n\n" +
            "Tým Tobiso");
    }

    private async Task<AccountEmailResult> SendThrottledAsync(AppUser user, string purpose, string subject, string body)
    {
        var cacheKey = $"account-mail:{purpose}:{user.Id}";
        if (_cache.TryGetValue(cacheKey, out _))
            return AccountEmailResult.Sent;

        try
        {
            await _email.SendAsync(user.Email, subject, body);
            _cache.Set(cacheKey, true, MailCooldown);
            return AccountEmailResult.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending {Purpose} email to user {UserId} failed", purpose, user.Id);
            return AccountEmailResult.Failed;
        }
    }

    private string BuildLink(string path, string token)
    {
        // Configured, never taken from the request's Host header - otherwise a forged Host could
        // make the reset link in a real user's inbox point at an attacker's site.
        var baseUrl = _config["App:PublicBaseUrl"] ?? _config["Api:BaseAddress"]
            ?? throw new InvalidOperationException("App:PublicBaseUrl is not configured.");
        return $"{baseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
    }

    // token = "{userId}.{expiryUnix}.{signature}"; the signature covers purpose, user, expiry,
    // security stamp and email, so it dies with any of them changing.
    private string CreateToken(AppUser user, string purpose, TimeSpan lifetime)
    {
        var exp = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        return $"{user.Id}.{exp}.{Sign(purpose, user.Id, exp, user)}";
    }

    private async Task<AppUser?> FindByTokenAsync(TobisoDbContext db, string token, string purpose)
    {
        var parts = (token ?? "").Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var userId) || !long.TryParse(parts[1], out var exp))
            return null;
        if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
            return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        var expected = Encoding.ASCII.GetBytes(Sign(purpose, userId, exp, user));
        var actual = Encoding.ASCII.GetBytes(parts[2]);
        return CryptographicOperations.FixedTimeEquals(expected, actual) ? user : null;
    }

    private string Sign(string purpose, int userId, long exp, AppUser user)
    {
        var mac = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{purpose}|{userId}|{exp}|{user.SecurityStamp}|{user.Email}"));
        return Convert.ToBase64String(mac).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
