using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Application.Features.Applications.Mappings;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Applications.Commands;

public record SubmitApplicationCommand(Guid ApplicationId) : ICommand<object>;

public class SubmitApplicationCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<SubmitApplicationCommand, object>
{
    public async Task<object> Handle(SubmitApplicationCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications
            .Include(a => a.Program).Include(a => a.Intake)
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.UserId != userId) throw AppException.Forbidden();
        if (app.Status != ApplicationStatus.Draft)
            throw new AppException("Only Draft applications can be submitted.", 422);

        var feeType = await db.FeeTypes
            .FirstOrDefaultAsync(f => f.Code == FeeCode.Application, ct)
            ?? throw new AppException("Application fee type not configured.", 500);

        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.ApplicationId == cmd.ApplicationId && i.FeeCode == FeeCode.Application, ct);

        if (invoice == null)
        {
            invoice = new Invoice
            {
                OrganizationId = tenant.OrganizationId,
                ApplicationId = cmd.ApplicationId,
                FeeTypeId = feeType.Id,
                FeeCode = FeeCode.Application,
                Amount = feeType.Amount
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(ct);
        }

        if (invoice.Status != InvoiceStatus.Paid)
            return new UnpaidInvoiceInfo(
                invoice.Id, invoice.FeeCode.ToString(), invoice.Amount,
                "Please pay the application fee to submit your application.");

        app.Submit();
        await db.SaveChangesAsync(ct);
        return ApplicationMapper.ToSummary(app, app.Program.Name, app.Intake.Name);
    }
}
