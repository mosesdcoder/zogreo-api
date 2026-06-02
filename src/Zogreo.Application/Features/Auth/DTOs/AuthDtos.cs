namespace Zogreo.Application.Features.Auth.DTOs;

public record AuthResult(string Token, string FullName, string Email, string Role);
public record SignupResult(string Message, string? DevOtp);
public record UserProfileDto(Guid Id, string FullName, string Email, string Phone, bool PhoneVerified, string Role);
