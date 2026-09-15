using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MobileCatalog.WebApi.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var username = configuration["Admin:Username"];
        var hash = configuration["Admin:PasswordHash"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(hash))
            return Problem("El acceso con usuario y contraseña todavía no está configurado.", statusCode: 503);

        var validPassword = false;
        try
        {
            validPassword = new PasswordHasher<string>().VerifyHashedPassword(username, hash, request.Password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { }
        if (!validPassword || !string.Equals(username, request.Username.Trim(), StringComparison.Ordinal))
            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok(new { username });
    }

    [HttpGet("session")]
    public IActionResult Session() => User.Identity?.IsAuthenticated == true
        ? Ok(new { username = User.Identity.Name })
        : Unauthorized();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}

public sealed class LoginRequest
{
    [Required, StringLength(120)] public string Username { get; init; } = string.Empty;
    [Required, StringLength(1024)] public string Password { get; init; } = string.Empty;
}
