using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Application.Features.Applications.Mappings;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Applications.Queries;

public record GetApplicationByIdQuery(Guid Id, bool AdminAllowed = false) : IQuery<ApplicationDetailDto>;

public class GetApplicationByIdQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetApplicationByIdQuery, ApplicationDetailDto>
{
    private static readonly string[] StatusLadder =
    [
        "Draft", "Submitted", "UnderReview", "DocsVerified",
        "OfferMade", "OfferAccepted", "FeesPaid", "MedicalsCleared", "Enrolled"
    ];

    public async Task<ApplicationDetailDto> Handle(GetApplicationByIdQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.AsNoTracking()
            .Include(a => a.Program).Include(a => a.Intake)
            .FirstOrDefaultAsync(a => a.Id == q.Id, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (!q.AdminAllowed || tenant.UserRole == Role.Applicant.ToString())
            if (app.UserId != userId) throw AppException.Forbidden();

        return new ApplicationDetailDto(
            app.Id, app.ProgramId, app.Program.Name, app.IntakeId, app.Intake.Name,
            app.Status.ToString(), app.PersonalJson, app.EducationHistoryJson,
            app.NextOfKinJson, app.HowDidYouHear, app.SubmittedAt, app.DecisionReason,
            ApplicationMapper.WhatNext(app.Status), StatusLadder, app.CreatedAt);
    }
}
