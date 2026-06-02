using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Application.Features.Applications.Mappings;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Applications.Commands;

public record CreateDraftCommand(Guid ProgramId, Guid IntakeId) : ICommand<ApplicationSummaryDto>;

public class CreateDraftCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<CreateDraftCommand, ApplicationSummaryDto>
{
    public async Task<ApplicationSummaryDto> Handle(CreateDraftCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var intake = await db.Intakes.Include(i => i.Program)
            .FirstOrDefaultAsync(i => i.Id == cmd.IntakeId && i.ProgramId == cmd.ProgramId && i.Active, ct)
            ?? throw AppException.NotFound("Intake or program not found.");

        var duplicate = await db.Applications.AnyAsync(a =>
            a.UserId == userId && a.IntakeId == cmd.IntakeId &&
            a.Status != ApplicationStatus.Withdrawn && a.Status != ApplicationStatus.Rejected, ct);
        if (duplicate)
            throw AppException.Conflict("You already have an active application for this intake.");

        var app = new Domain.Entities.Application
        {
            OrganizationId = tenant.OrganizationId,
            UserId = userId,
            ProgramId = cmd.ProgramId,
            IntakeId = cmd.IntakeId
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync(ct);

        return ApplicationMapper.ToSummary(app, intake.Program.Name, intake.Name);
    }
}
