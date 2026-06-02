using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetReconciliationQuery(string? Status, int Page, int PageSize = 20) : IQuery<object>;

public class GetReconciliationQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetReconciliationQuery, object>
{
    public async Task<object> Handle(GetReconciliationQuery q, CancellationToken ct)
    {
        var query = db.Payments.AsNoTracking().Include(p => p.Invoice).AsQueryable();
        if (q.Status != null && Enum.TryParse<PaymentStatus>(q.Status, true, out var s))
            query = query.Where(p => p.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(p => new AdminPaymentItem(p.Id, p.Reference, p.Invoice.FeeCode.ToString(), p.Status.ToString(), p.AmountGross, p.ProviderFee, p.TechnologyFee, p.AmountNetToSchool, p.Channel.ToString(), p.CompletedAt))
            .ToListAsync(ct);

        var totals = await query.Where(p => p.Status == PaymentStatus.Success)
            .GroupBy(_ => 1)
            .Select(g => new { TotalGross = g.Sum(p => p.AmountGross), TotalTechFee = g.Sum(p => p.TechnologyFee), TotalNet = g.Sum(p => p.AmountNetToSchool) })
            .FirstOrDefaultAsync(ct);

        return new { total, page = q.Page, items, totals };
    }
}
