using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PriceTracker.Application.DTOs;
using PriceTracker.Application.Services;
using PriceTracker.Application.Validators;

namespace PriceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Kullanıcılar")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CreateUserRequestValidator _createValidator = new();
    private readonly LoginUserRequestValidator _loginValidator = new();
    private readonly UpdateUserProfileRequestValidator _updateValidator = new();

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var user = await _userService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Mevcut kullanıcı girişi.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginUserRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var user = await _userService.LoginAsync(request, ct);
        return user is null
            ? Unauthorized(new { message = "E-posta veya şifre hatalı." })
            : Ok(user);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        Guid id,
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var user = await _userService.UpdateProfileAsync(id, request, ct);
            return user is null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
