using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Application.Features.Documents.DTOs;
using Zogreo.Application.Features.Payments.DTOs;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetApplicationDetailQuery(Guid Id) : IQuery<AdminAppDetail>;

public class GetApplicationDetailQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetApplicationDetailQuery, AdminAppDetail>
{
    public async Task<AdminAppDetail> Handle(GetApplicationDetailQuery q, CancellationToken ct)
    {
        var app = await db.Applications
            .Include(a => a.User).Include(a => a.Program).Include(a => a.Intake)
            .Include(a => a.Documents).Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == q.Id, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.Status == Domain.Enums.ApplicationStatus.Submitted)
        {
            app.MoveToReview();
            await db.SaveChangesAsync(ct);
        }

        return new AdminAppDetail(
            app.Id, app.UserId, app.User.FullName, app.User.Email, app.User.Phone,
            app.Program.Name, app.Intake.Name, app.Status.ToString(),
            app.PersonalJson, app.EducationHistoryJson, app.NextOfKinJson, app.HowDidYouHear,
            app.SubmittedAt, app.DecisionReason,
            app.Documents.Select(d => new DocumentDto(d.Id, d.Type.ToString(), d.FileUrl, d.OriginalFileName, d.Status.ToString(), d.ReviewReason, d.CreatedAt)),
            app.Invoices.Select(i => new InvoiceDto(i.Id, i.FeeCode.ToString(), i.Amount, i.AmountPaid, i.Status.ToString(), i.DueAt)));
    }
}
