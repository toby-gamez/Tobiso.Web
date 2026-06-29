using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.App.Authentication;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string GenerateStudentToken(AppUser user)
    {
        var secret  = _config["Auth:Jwt:Secret"]!;
        var issuer  = _config["Auth:Jwt:Issuer"]   ?? "tobiso";
        var audience = _config["Auth:Jwt:Audience"] ?? "tobiso";
        var expiry  = DateTime.UtcNow.AddDays(30);

        return CreateToken(secret, user.Id.ToString(), user.DisplayName, issuer, audience, expiry,
            extraClaims: new Dictionary<string, string>
            {
                ["email"]   = user.Email,
                ["role"]    = "student",
                ["credits"] = user.Credits.ToString()
            });
    }

    public string? GenerateToken(string username, string password)
    {
        var expectedUsername = _config["Auth:Basic:Username"];
        var expectedPassword = _config["Auth:Basic:Password"];
        var userId           = _config["Auth:Basic:UserId"] ?? "1";

        if (username != expectedUsername || password != expectedPassword)
        {
            _logger.LogWarning("JWT login failed — invalid credentials for user {Username}", username);
            return null;
        }

        var secret = _config["Auth:Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Auth:Jwt:Secret is not configured — JWT login unavailable");
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var issuer   = _config["Auth:Jwt:Issuer"]   ?? "tobiso";
        var audience = _config["Auth:Jwt:Audience"] ?? "tobiso";
        var expiry   = DateTime.UtcNow.AddHours(
            int.TryParse(_config["Auth:Jwt:ExpiryHours"], out var h) ? h : 24);

        return CreateToken(secret, userId, username, issuer, audience, expiry);
    }

    private string CreateToken(string secret, string userId, string username,
        string issuer, string audience, DateTime expiry,
        Dictionary<string, string>? extraClaims = null)
    {
        var header = JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" });

        var payloadDict = new Dictionary<string, object>
        {
            ["sub"]            = userId,
            ["unique_name"]    = username,
            ["name"]           = username,
            ["nameidentifier"] = userId,
            ["jti"]            = Guid.NewGuid().ToString(),
            ["exp"]            = new DateTimeOffset(expiry).ToUnixTimeSeconds(),
            ["iss"]            = issuer,
            ["aud"]            = audience
        };
        if (extraClaims != null)
            foreach (var kv in extraClaims)
                payloadDict[kv.Key] = kv.Value;

        var payload = JsonSerializer.Serialize(payloadDict);

        var headerBase64   = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadBase64  = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput   = $"{headerBase64}.{payloadBase64}";
        var keyBytes       = Encoding.UTF8.GetBytes(secret);
        var signature      = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(signingInput));
        var signatureBase64 = Base64UrlEncode(signature);

        var token = $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        _logger.LogInformation("JWT token issued for user {Username}, expires {Expiry:u}", username, expiry);
        return token;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public record LoginRequest(string Username, string Password);