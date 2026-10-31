using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Tobiso.Api.Authentication;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Authentication;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwtService;
    private readonly IUserService _userService;
    private readonly IAccountService _accountService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtTokenService jwtService, IUserService userService, IAccountService accountService,
        ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _userService = userService;
        _accountService = accountService;
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
            var token = _jwtService.GenerateToken(request.Username, request.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            if (token == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(new { token });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Login failed - configuration error");
            return StatusCode(503, new { message = "Login is temporarily unavailable." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed - unexpected error");
            return StatusCode(500, new { message = "Login failed." });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email a heslo jsou povinné." });

        if (!req.Email.Contains('@') || req.Email.Length > 256)
            return BadRequest(new { message = "Zadej platný e-mail." });

        if (req.Password.Length < IAccountService.MinPasswordLength || req.Password.Length > IAccountService.MaxPasswordLength)
            return BadRequest(new { message = "Heslo musí mít alespoň 10 znaků." });

        if (!req.AcceptedTerms)
            return BadRequest(new { message = "Pro registraci je nutný souhlas s Podmínkami použití a Zásadami ochrany osobních údajů." });

        var displayName = string.IsNullOrWhiteSpace(req.DisplayName) ? req.Email.Trim() : req.DisplayName.Trim();
        if (displayName.Length > 100) displayName = displayName[..100];

        // No token here: the account can't sign in until the emailed link is opened. The answer is
        // the same whether or not the email already had an account.
        var result = await _accountService.RegisterAsync(req.Email, displayName, req.Password);
        return result == AccountEmailResult.Sent
            ? Ok(new { message = "Poslali jsme ti e-mail s odkazem pro potvrzení účtu." })
            : StatusCode(503, new { message = "E-mail se nepodařilo odeslat. Zkus to prosím později." });
    }

    // For API clients that send the token as a Bearer header; the website uses /session.
    [HttpPost("student-login")]
    [AllowAnonymous]
    public async Task<IActionResult> StudentLogin([FromBody] LoginRequest req)
    {
        var (user, error) = await CheckStudentCredentialsAsync(req);
        if (user == null) return error!;

        var token = _jwtService.GenerateStudentToken(user);
        return Ok(new StudentLoginResponse(token, user.DisplayName, user.Credits));
    }

    // Website sign-in: the token goes into an HttpOnly cookie that page script can't read.
    [HttpPost("session")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateSession([FromBody] LoginRequest req)
    {
        var (user, error) = await CheckStudentCredentialsAsync(req);
        if (user == null) return error!;

        SetStudentCookie(user);
        return Ok(new { displayName = user.DisplayName });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        SessionCookies.Delete(Response, SessionCookies.Student);
        return NoContent();
    }

    // Ends every session of the account - other browsers and devices included.
    [HttpPost("logout-everywhere")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
    public async Task<IActionResult> LogoutEverywhere()
    {
        if (!AuthPolicies.IsStudent(User) ||
            !int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Forbid();

        await _userService.RotateSecurityStampAsync(userId);
        SessionCookies.Delete(Response, SessionCookies.Student);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@') || req.Email.Length > 256)
            return BadRequest(new { message = "Zadej platný e-mail." });

        // Same answer whether or not the account exists (and even if mailing failed - it's logged),
        // so this can't be used to find out who has an account.
        await _accountService.RequestPasswordResetAsync(req.Email);
        return Ok(new { message = "Pokud k tomuto e-mailu existuje účet, poslali jsme na něj odkaz pro nastavení nového hesla." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrEmpty(req.Password) ||
            req.Password.Length < IAccountService.MinPasswordLength || req.Password.Length > IAccountService.MaxPasswordLength)
            return BadRequest(new { message = "Heslo musí mít alespoň 10 znaků." });

        var user = await _accountService.ResetPasswordAsync(req.Token ?? "", req.Password);
        if (user == null)
            return BadRequest(new { message = "Odkaz je neplatný nebo vypršel. Požádej o nový." });

        // The reset rotated the stamp (all old sessions are gone); sign this browser in fresh.
        SetStudentCookie(user);
        return Ok(new { displayName = user.DisplayName });
    }

    private async Task<(AppUser? User, IActionResult? Error)> CheckStudentCredentialsAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return (null, BadRequest(new { message = "Email a heslo jsou povinné." }));

        var user = await _userService.LoginAsync(req.Username.Trim(), req.Password);
        if (user == null)
            return (null, Unauthorized(new { message = "Nesprávný email nebo heslo." }));

        if (user.EmailConfirmedAt == null)
        {
            await _accountService.SendVerificationAsync(user);
            return (null, StatusCode(403, new
            {
                code = "email_unverified",
                message = "Nejdřív potvrď svůj e-mail - poslali jsme ti na něj odkaz."
            }));
        }

        return (user, null);
    }

    private void SetStudentCookie(AppUser user) =>
        SessionCookies.Append(Response, SessionCookies.Student, _jwtService.GenerateStudentToken(user),
            DateTimeOffset.UtcNow.Add(JwtTokenService.StudentTokenLifetime));

    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
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
    public IActionResult GoogleLogin([FromQuery] bool terms = false)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        // Carried through the OAuth round-trip so the callback knows whether the user consented.
        if (terms) properties.Items["terms"] = "1";
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
        var picture    = result.Principal!.FindFirst("picture")?.Value;

        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            return Redirect("/prihlaseni?error=google");

        var acceptedTerms = result.Properties?.Items.TryGetValue("terms", out var t) == true && t == "1";
        var user = await _userService.FindOrCreateGoogleUserAsync(googleId, email, name ?? email, picture, acceptedTerms);
        if (user == null)
        {
            await HttpContext.SignOutAsync("TempCookie");
            return Redirect("/registrace?error=terms");
        }
        await HttpContext.SignOutAsync("TempCookie");

        // Straight into the HttpOnly session cookie - the token never appears in a URL or to script.
        SetStudentCookie(user);
        return Redirect("/");
    }

    // For API clients (mobile app, One Tap) that already hold a Google ID token; the website uses the redirect flow.
    [HttpPost("google-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleToken([FromBody] GoogleTokenRequest req, [FromServices] IConfiguration config)
    {
        // Web client ID plus optional extra audiences (e.g. native iOS/Android client IDs).
        // ExtraClientIds may be an array or a single comma-separated string (env var / app setting).
        var extraSection = config.GetSection("Google:ExtraClientIds");
        var audiences = new[] { config["Google:ClientId"] }
            .Concat(extraSection.Get<string[]>() ?? [])
            .Concat((extraSection.Value ?? "").Split(',', StringSplitOptions.TrimEntries))
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a!)
            .ToList();
        if (audiences.Count == 0)
        {
            _logger.LogError("Google:ClientId is not configured; google-token sign-in is unavailable.");
            return StatusCode(503, new { message = "Přihlášení přes Google není dostupné." });
        }
        if (string.IsNullOrWhiteSpace(req.IdToken))
            return BadRequest(new { message = "Neplatný požadavek." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = audiences });
        }
        catch (Exception ex) when (ex is InvalidJwtException or FormatException or ArgumentException or Newtonsoft.Json.JsonException)
        {
            // Messages can embed attacker-controlled token content, so log only the exception type.
            _logger.LogWarning("Google ID token rejected ({ErrorType})", ex.GetType().Name);
            return Unauthorized(new { message = "Přihlášení přes Google se nezdařilo." });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogError(ex, "Could not fetch Google signing certificates.");
            return StatusCode(503, new { message = "Přihlášení přes Google je dočasně nedostupné." });
        }

        // An unverified address must never be linked to an existing account.
        if (!payload.EmailVerified || string.IsNullOrEmpty(payload.Email))
            return Unauthorized(new { message = "E-mail Google účtu není ověřený." });

        var user = await _userService.FindOrCreateGoogleUserAsync(payload.Subject, payload.Email,
            payload.Name ?? payload.Email, payload.Picture, req.AcceptedTerms);
        if (user == null)
            return BadRequest(new { code = "terms_required", message = "Pro vytvoření účtu je nutné přijmout podmínky." });

        var token = _jwtService.GenerateStudentToken(user);
        return Ok(new StudentLoginResponse(token, user.DisplayName, user.Credits));
    }

    [HttpPost("daily-bonus")]
    [Authorize(Policy = AuthPolicies.AnyUser)]
    public async Task<IActionResult> ClaimDailyBonus()
    {
        var role = User.FindFirst("role")?.Value;
        if (role != "student") return Forbid();

        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        // Tracked separately from LastLoginAt (which changes on every login, not on every
        // claim) so a valid 30-day JWT can't be replayed against this endpoint to farm
        // unlimited credits without ever logging in again.
        if (user.LastDailyBonusAt?.Date == DateTime.UtcNow.Date)
            return Conflict(new { message = "Denní bonus byl již dnes vybrán." });

        var claimed = await _userService.ClaimDailyBonusAsync(userId, 20);
        if (!claimed)
            return Conflict(new { message = "Denní bonus byl již dnes vybrán." });

        var updated = await _userService.GetByIdAsync(userId);
        return Ok(new { credits = updated?.Credits ?? 0 });
    }
}

public record GoogleTokenRequest(string IdToken, bool AcceptedTerms = false);
