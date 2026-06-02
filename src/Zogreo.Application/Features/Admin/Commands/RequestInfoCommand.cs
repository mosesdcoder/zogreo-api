using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Admin.Commands;

public record RequestInfoCommand(Guid ApplicationId, string Reason) : ICommand<Unit>;

public class RequestInfoCommandHandler(
    IApplicationDbContext db,
    INotificationOutbox outbox) : ICommandHandler<RequestInfoCommand, Unit>
{
    public async Task<Unit> Handle(RequestInfoCommand cmd, CancellationToken ct)
    {
        var app = await db.Applications.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        app.RequestInfo(cmd.Reason);
        await db.SaveChangesAsync(ct);

        var user = app.User;
        await outbox.QueueSmsAsync(user.Id, user.Phone, "info_requested",
            $"Additional info needed for your application: {cmd.Reason}");
        await outbox.QueueEmailAsync(user.Id, user.Email, "info_requested",
            "Additional Information Required",
            $"Hi {user.FullName}, additional information has been requested. Reason: {cmd.Reason}");

        return new Unit();
    }
}
