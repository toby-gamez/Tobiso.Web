using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(
            int.TryParse(_config["Auth:Jwt:ExpiryHours"], out var h) ? h : 24);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Name,                    username),
            new Claim(ClaimTypes.NameIdentifier,          userId),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Auth:Jwt:Issuer"]   ?? "tobiso",
            audience:           _config["Auth:Jwt:Audience"] ?? "tobiso",
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        _logger.LogInformation("JWT token issued for user {Username}, expires {Expiry:u}", username, expiry);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Username, string Password);