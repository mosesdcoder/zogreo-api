using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Payments.Queries;

public record ApplicationPaymentDto(
    Guid Id,
    string Reference,
    string FeeCode,
    string Channel,
    string Status,
    decimal AmountGross,
    decimal ProviderFee,
    decimal TechnologyFee,
    decimal AmountNetToSchool,
    DateTimeOffset? CompletedAt);

public record GetApplicationPaymentsQuery(Guid ApplicationId) : IQuery<List<ApplicationPaymentDto>>;

public class GetApplicationPaymentsQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetApplicationPaymentsQuery, List<ApplicationPaymentDto>>
{
    public async Task<List<ApplicationPaymentDto>> Handle(GetApplicationPaymentsQuery query, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == query.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.UserId != userId && tenant.UserRole == Role.Applicant.ToString())
            throw AppException.Forbidden();

        var payments = await db.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.ApplicationId == query.ApplicationId)
            .OrderByDescending(p => p.CompletedAt.HasValue ? p.CompletedAt : p.CreatedAt)
            .Select(p => new ApplicationPaymentDto(
                p.Id,
                p.Reference,
                p.Invoice.FeeCode.ToString(),
                p.Channel.ToString(),
                p.Status.ToString(),
                p.AmountGross,
                p.ProviderFee,
                p.TechnologyFee,
                p.AmountNetToSchool,
                p.CompletedAt))
            .ToListAsync(ct);

        return payments;
    }
}
