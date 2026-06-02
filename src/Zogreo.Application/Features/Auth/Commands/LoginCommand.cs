using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.DTOs;
using Zogreo.Domain.Entities;

namespace Zogreo.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : ICommand<AuthResult>;

public class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwt) : ICommandHandler<LoginCommand, AuthResult>
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<AuthResult> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == cmd.Email.Trim().ToLowerInvariant(), ct)
            ?? throw new AppException("Invalid email or password.", 401);

        if (!user.Active) throw new AppException("Account is disabled.", 403);
        if (!user.PhoneVerified) throw new AppException("Phone not verified. Please verify your OTP first.", 403);

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, cmd.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new AppException("Invalid email or password.", 401);

        return new AuthResult(jwt.Generate(user), user.FullName, user.Email, user.Role.ToString());
    }
}
