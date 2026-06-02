using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Auth.DTOs;

namespace Zogreo.Application.Features.Auth.Commands;

public record VerifyOtpCommand(string Phone, string Code) : ICommand<AuthResult>;

public class VerifyOtpCommandHandler(
    IApplicationDbContext db,
    IOtpService otp,
    IJwtTokenService jwt) : ICommandHandler<VerifyOtpCommand, AuthResult>
{
    public async Task<AuthResult> Handle(VerifyOtpCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNormalizer.Normalize(cmd.Phone);
        var valid = await otp.VerifyAndConsumeAsync(phone, cmd.Code);
        if (!valid) throw new AppException("Invalid or expired OTP.", 400);

        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone, ct)
            ?? throw AppException.NotFound("User not found.");

        user.PhoneVerified = true;
        await db.SaveChangesAsync(ct);

        return new AuthResult(jwt.Generate(user), user.FullName, user.Email, user.Role.ToString());
    }
}
