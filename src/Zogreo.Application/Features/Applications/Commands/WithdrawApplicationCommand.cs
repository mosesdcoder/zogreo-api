using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Applications.Commands;

public record WithdrawApplicationCommand(Guid ApplicationId) : ICommand<Unit>;

public class WithdrawApplicationCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<WithdrawApplicationCommand, Unit>
{
    public async Task<Unit> Handle(WithdrawApplicationCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.UserId != userId) throw AppException.Forbidden();

        app.Withdraw();
        await db.SaveChangesAsync(ct);
        return new Unit();
    }
}
