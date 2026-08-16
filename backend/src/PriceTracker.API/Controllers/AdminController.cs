using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;
using PriceTracker.Application.Validators;
using PriceTracker.Infrastructure.Admin;

namespace PriceTracker.API.Controllers;

[ApiController]
[Route("api/admin")]
[Tags("Yönetici")]
public class AdminController : ControllerBase
{
    private readonly AdminAuthService _adminAuth;
    private readonly UserService _userService;

    public AdminController(AdminAuthService adminAuth, UserService userService)
    {
        _adminAuth = adminAuth;
        _userService = userService;
    }

    /// <summary>Yönetici girişi — token alır.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest? request,
        CancellationToken ct)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-posta ve şifre gerekli." });

        var result = await _adminAuth.LoginAsync(request, ct);
        if (result is null)
            return Unauthorized(new { message = "Yönetici girişi başarısız." });

        return Ok(result);
    }

    /// <summary>Yönetici çıkışı.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _adminAuth.LogoutAsync(GetToken(), ct);
        return NoContent();
    }

    /// <summary>Kayıtlı kullanıcı listesi (ad, soyad, e-posta) — yalnızca yönetici.</summary>
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<RegisteredUserListItemDto>>> ListUsers(CancellationToken ct)
    {
        var admin = await _adminAuth.ValidateTokenAsync(GetToken(), ct);
        if (admin is null)
            return Unauthorized(new { message = "Yönetici girişi gerekli." });

        return Ok(await _userService.ListRegisteredUsersAsync(ct));
    }

    private string? GetToken()
    {
        if (Request.Headers.TryGetValue("X-Admin-Token", out var header) &&
            !string.IsNullOrWhiteSpace(header))
            return header.ToString();

        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..].Trim();

        return null;
    }
}
