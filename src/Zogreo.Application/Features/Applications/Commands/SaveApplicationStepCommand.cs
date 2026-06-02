using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Application.Features.Applications.Mappings;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Applications.Commands;

public record SaveApplicationStepCommand(
    Guid ApplicationId,
    string? PersonalJson,
    string? EducationHistoryJson,
    string? NextOfKinJson,
    string? HowDidYouHear) : ICommand<ApplicationSummaryDto>;

public class SaveApplicationStepCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<SaveApplicationStepCommand, ApplicationSummaryDto>
{
    public async Task<ApplicationSummaryDto> Handle(SaveApplicationStepCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications
            .Include(a => a.Program).Include(a => a.Intake)
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.UserId != userId) throw AppException.Forbidden();
        if (app.Status != ApplicationStatus.Draft)
            throw new AppException("Only Draft applications can be edited.", 422);

        if (cmd.PersonalJson != null) app.PersonalJson = cmd.PersonalJson;
        if (cmd.EducationHistoryJson != null) app.EducationHistoryJson = cmd.EducationHistoryJson;
        if (cmd.NextOfKinJson != null) app.NextOfKinJson = cmd.NextOfKinJson;
        if (cmd.HowDidYouHear != null) app.HowDidYouHear = cmd.HowDidYouHear;

        await db.SaveChangesAsync(ct);
        return ApplicationMapper.ToSummary(app, app.Program.Name, app.Intake.Name);
    }
}
