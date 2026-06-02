using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Admin.Commands;

public record RejectApplicationCommand(Guid ApplicationId, string Reason) : ICommand<Unit>;

public class RejectApplicationCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    INotificationOutbox outbox) : ICommandHandler<RejectApplicationCommand, Unit>
{
    public async Task<Unit> Handle(RejectApplicationCommand cmd, CancellationToken ct)
    {
        var adminId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        app.Reject(adminId, cmd.Reason);
        await db.SaveChangesAsync(ct);

        var user = app.User;
        await outbox.QueueEmailAsync(user.Id, user.Email, "rejected",
            "Application Decision",
            $"Hi {user.FullName}, unfortunately your application was not successful. Reason: {cmd.Reason}");

        return new Unit();
    }
}
