using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tobiso.Api.Authentication;

namespace Tobiso.Web.App.Admin.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("auth")]
public class AuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IOptionsMonitor<ManualJwtAuthOptions> jwtOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("verify")]
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    public IActionResult VerifyCredentials()
    {
        // If we reach here, authentication was successful
        return Ok(new { authenticated = true, user = User.Identity?.Name });
    }

    // Signs the admin in: the main app issues the JWT, which is kept in an HttpOnly cookie on this
    // host so page script (and any XSS) can never read it.
    [HttpPost("session")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateSession([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        string? token;
        try
        {
            token = await RequestJwtTokenAsync(request.Username, request.Password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin login failed - token request error");
            return StatusCode(503, new { message = "Login is temporarily unavailable." });
        }

        if (token == null)
            return Unauthorized(new { message = "Invalid username or password." });

        var principal = ManualJwtAuthHandler.ValidateToken(token, jwtOptions.Get("Bearer"));
        if (!AuthPolicies.IsAdmin(principal) || SessionCookies.GetExpiry(principal) is not { } expiry)
            return Unauthorized(new { message = "Invalid username or password." });

        SessionCookies.Append(Response, SessionCookies.Admin, token, expiry);
        return Ok(new { });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        SessionCookies.Delete(Response, SessionCookies.Admin);
        return NoContent();
    }

    // Calls POST /api/auth/login on the main app. Returns null on invalid credentials.
    private async Task<string?> RequestJwtTokenAsync(string username, string password)
    {
        var baseAddress = configuration["Api:BaseAddress"];
        if (string.IsNullOrEmpty(baseAddress))
            throw new InvalidOperationException("API base address is not configured.");

        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        // The admin password travels on this call - certificate checks stay on outside development.
        if (environment.IsDevelopment())
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await httpClient.PostAsync("/api/auth/login",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.BadRequest)
            return null;
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() : null;
    }
}

public record AdminLoginRequest(string Username, string Password);
