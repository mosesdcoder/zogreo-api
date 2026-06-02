using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Commands;

public record UpdateIntakeCommand(Guid Id, string? Name, DateTimeOffset? OpensAt, DateTimeOffset? ClosesAt, DateTimeOffset? StartsAt, int? Capacity, bool? Active)
    : ICommand<IntakeDto>;

public class UpdateIntakeCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateIntakeCommand, IntakeDto>
{
    public async Task<IntakeDto> Handle(UpdateIntakeCommand cmd, CancellationToken ct)
    {
        var intake = await db.Intakes.Include(i => i.Program)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, ct)
            ?? throw AppException.NotFound("Intake not found.");

        if (cmd.Name != null) intake.Name = cmd.Name;
        if (cmd.OpensAt.HasValue) intake.OpensAt = cmd.OpensAt.Value;
        if (cmd.ClosesAt.HasValue) intake.ClosesAt = cmd.ClosesAt.Value;
        if (cmd.StartsAt.HasValue) intake.StartsAt = cmd.StartsAt.Value;
        if (cmd.Capacity.HasValue) intake.Capacity = cmd.Capacity.Value;
        if (cmd.Active.HasValue) intake.Active = cmd.Active.Value;

        await db.SaveChangesAsync(ct);
        return new IntakeDto(intake.Id, intake.ProgramId, intake.Program.Name, intake.Name, intake.OpensAt, intake.ClosesAt, intake.StartsAt, intake.Capacity, intake.Active);
    }
}
