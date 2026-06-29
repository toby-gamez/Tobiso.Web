using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Authentication;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwtService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtTokenService jwtService, IUserService userService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("verify")]
    [Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
    public IActionResult VerifyCredentials()
    {
        return Ok(new { authenticated = true, user = User.Identity?.Name });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        try
        {
            var token = _jwtService.GenerateToken(request.Username, request.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(new { token });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Login failed — configuration error");
            return StatusCode(503, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed — unexpected error");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email a heslo jsou povinné." });

        var user = await _userService.RegisterAsync(req.Email, req.DisplayName ?? req.Email, req.Password);
        if (user == null)
            return Conflict(new { message = "Tento email je již zaregistrován." });

        var token = _jwtService.GenerateStudentToken(user);
        return Ok(new StudentLoginResponse(token, user.DisplayName, user.Credits));
    }

    [HttpPost("student-login")]
    [AllowAnonymous]
    public async Task<IActionResult> StudentLogin([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email a heslo jsou povinné." });

        var user = await _userService.LoginAsync(req.Username, req.Password);
        if (user == null)
            return Unauthorized(new { message = "Nesprávný email nebo heslo." });

        var token = _jwtService.GenerateStudentToken(user);
        return Ok(new StudentLoginResponse(token, user.DisplayName, user.Credits));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var role = User.FindFirst("role")?.Value;
        if (role == "student")
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(new StudentProfileDto(user.DisplayName, user.Email, user.Credits, "student"));
        }
        return Ok(new StudentProfileDto(User.Identity?.Name ?? "", "", 0, "admin"));
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("TempCookie");
        if (!result.Succeeded)
        {
            _logger.LogWarning("Google OAuth callback failed: {Error}", result.Failure?.Message);
            return Redirect("/prihlaseni?error=google");
        }

        var googleId   = result.Principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email      = result.Principal!.FindFirst(ClaimTypes.Email)?.Value;
        var name       = result.Principal!.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            return Redirect("/prihlaseni?error=google");

        var user = await _userService.FindOrCreateGoogleUserAsync(googleId, email, name ?? email);
        var token = _jwtService.GenerateStudentToken(user);

        await HttpContext.SignOutAsync("TempCookie");

        return Redirect($"/google-auth-complete?token={Uri.EscapeDataString(token)}");
    }

    [HttpPost("daily-bonus")]
    [Authorize]
    public async Task<IActionResult> ClaimDailyBonus()
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "student") return Forbid();

        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.LastLoginAt?.Date == DateTime.UtcNow.Date)
            return Conflict(new { message = "Denní bonus byl již dnes vybrán." });

        await _userService.AddCreditsAsync(userId, 20, "daily_bonus");
        return Ok(new { credits = user.Credits + 20 });
    }
}
