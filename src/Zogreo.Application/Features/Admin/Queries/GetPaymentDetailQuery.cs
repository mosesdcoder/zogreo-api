using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetPaymentDetailQuery(Guid Id) : IQuery<AdminPaymentItem>;

public class GetPaymentDetailQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetPaymentDetailQuery, AdminPaymentItem>
{
    public async Task<AdminPaymentItem> Handle(GetPaymentDetailQuery q, CancellationToken ct)
        => await db.Payments.AsNoTracking()
            .Include(p => p.Invoice)
            .Where(p => p.Id == q.Id)
            .Select(p => new AdminPaymentItem(p.Id, p.Reference, p.Invoice.FeeCode.ToString(), p.Status.ToString(), p.AmountGross, p.ProviderFee, p.TechnologyFee, p.AmountNetToSchool, p.Channel.ToString(), p.CompletedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Payment not found.");
}
