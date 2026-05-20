using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Web.App.Authentication;

namespace Tobiso.Web.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwtService;

    public AuthController(JwtTokenService jwtService)
    {
        _jwtService = jwtService;
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
            return StatusCode(503, new { message = ex.Message });
        }
    }
}