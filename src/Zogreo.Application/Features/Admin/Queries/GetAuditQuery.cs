using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetAuditQuery(int Page, int PageSize = 50) : IQuery<object>;

public class GetAuditQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAuditQuery, object>
{
    public async Task<object> Handle(GetAuditQuery q, CancellationToken ct)
    {
        var total = await db.AuditLogs.CountAsync(ct);
        var items = await db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.At)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .ToListAsync(ct);
        return new { total, page = q.Page, items };
    }
}
