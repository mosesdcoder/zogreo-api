using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Auth.Commands;

public record ResendOtpCommand(string Phone) : ICommand<Unit>;

public class ResendOtpCommandHandler(
    IApplicationDbContext db,
    IOtpService otp,
    INotificationOutbox outbox) : ICommandHandler<ResendOtpCommand, Unit>
{
    public async Task<Unit> Handle(ResendOtpCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNormalizer.Normalize(cmd.Phone);
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone, ct)
            ?? throw AppException.NotFound("User not found.");

        if (user.PhoneVerified) throw AppException.Conflict("Phone is already verified.");

        var code = await otp.GenerateAndStoreAsync(phone);
        await outbox.QueueSmsAsync(user.Id, phone, "otp",
            $"Your Zogreo verification code is {code}. Valid for 10 minutes.");

        return new Unit();
    }
}
