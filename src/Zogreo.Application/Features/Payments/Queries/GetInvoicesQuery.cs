using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Payments.Queries;

public record GetInvoicesQuery(Guid ApplicationId) : IQuery<List<InvoiceDto>>;

public class GetInvoicesQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetInvoicesQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetInvoicesQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == q.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.UserId != userId && tenant.UserRole == Role.Applicant.ToString())
            throw AppException.Forbidden();

        return await db.Invoices.AsNoTracking()
            .Where(i => i.ApplicationId == q.ApplicationId)
            .Select(i => new InvoiceDto(i.Id, i.FeeCode.ToString(), i.Amount, i.AmountPaid, i.Status.ToString(), i.DueAt))
            .ToListAsync(ct);
    }
}
