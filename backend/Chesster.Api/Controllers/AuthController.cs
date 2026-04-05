using System.Security.Claims;
using Chesster.Api.Models.DTOs;
using Chesster.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chesster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IJwtService jwtService, IConfiguration configuration)
    {
        _authService = authService;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserByEmailAsync(User.FindFirstValue(ClaimTypes.Email) ?? "");
        if (user == null) return NotFound();

        return Ok(new UserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Auth")
        };
        return Challenge(props, "Google");
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
            return BadRequest(new { message = "Google authentication failed" });

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
        var name = result.Principal?.FindFirstValue(ClaimTypes.Name) ?? "Google User";
        var providerKey = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Email not provided by Google" });

        var user = await _authService.FindOrCreateOAuthUserAsync("Google", providerKey, email, name);
        var token = _jwtService.GenerateToken(user);

        return Redirect($"http://localhost:5173/oauth-callback?token={token}");
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin([FromQuery] string? returnUrl = null)
    {
        var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = Url.Action("GitHubCallback", "Auth")
        };
        return Challenge(props, "GitHub");
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback()
    {
        var result = await HttpContext.AuthenticateAsync("GitHub");
        if (!result.Succeeded)
            return BadRequest(new { message = "GitHub authentication failed" });

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
        var name = result.Principal?.FindFirstValue("urn:github:login") ?? "GitHub User";
        var providerKey = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Email not provided by GitHub" });

        var user = await _authService.FindOrCreateOAuthUserAsync("GitHub", providerKey, email, name);
        var token = _jwtService.GenerateToken(user);

        return Redirect($"http://localhost:5173/oauth-callback?token={token}");
    }
}