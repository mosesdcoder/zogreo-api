using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.Commands;
using Zogreo.Application.Features.Auth.Queries;

namespace Zogreo.Api.Controllers;

/// <summary>Authentication and user profile</summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Register a new applicant. Returns a devOtp in Development.</summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Signup(SignupRequest req, CancellationToken ct)
        => Ok(await sender.Send(new SignupCommand(req.FullName, req.Email, req.Phone, req.Password), ct));

    /// <summary>Verify phone with the OTP received at signup. Returns a JWT token.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest req, CancellationToken ct)
        => Ok(await sender.Send(new VerifyOtpCommand(req.Phone, req.Code), ct));

    /// <summary>Resend OTP to an unverified phone number.</summary>
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendOtp(ResendOtpRequest req, CancellationToken ct)
    {
        await sender.Send(new ResendOtpCommand(req.Phone), ct);
        return Ok(new { message = "OTP resent." });
    }

    /// <summary>Login with email and password. Returns a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
        => Ok(await sender.Send(new LoginCommand(req.Email, req.Password), ct));

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await sender.Send(new GetMeQuery(), ct));

    /// <summary>Update the current user's name or phone number.</summary>
    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateProfileCommand(req.FullName, req.Phone), ct));
}

public record SignupRequest(string FullName, string Email, string Phone, string Password);
public record VerifyOtpRequest(string Phone, string Code);
public record ResendOtpRequest(string Phone);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string? FullName, string? Phone);
