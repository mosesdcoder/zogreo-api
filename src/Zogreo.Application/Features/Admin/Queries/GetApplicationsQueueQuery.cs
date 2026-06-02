using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetApplicationsQueueQuery(string? Status, Guid? ProgramId, Guid? IntakeId, int Page, int PageSize = 20)
    : IQuery<PagedResult<AdminAppListItem>>;

public class GetApplicationsQueueQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetApplicationsQueueQuery, PagedResult<AdminAppListItem>>
{
    public async Task<PagedResult<AdminAppListItem>> Handle(GetApplicationsQueueQuery q, CancellationToken ct)
    {
        var query = db.Applications.AsNoTracking()
            .Include(a => a.User).Include(a => a.Program).Include(a => a.Intake)
            .AsQueryable();

        if (q.Status != null && Enum.TryParse<ApplicationStatus>(q.Status, true, out var s))
            query = query.Where(a => a.Status == s);
        if (q.ProgramId.HasValue) query = query.Where(a => a.ProgramId == q.ProgramId.Value);
        if (q.IntakeId.HasValue) query = query.Where(a => a.IntakeId == q.IntakeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(a => new AdminAppListItem(a.Id, a.User.FullName, a.User.Email, a.Program.Name, a.Intake.Name, a.Status.ToString(), a.SubmittedAt, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminAppListItem>(total, q.Page, items);
    }
}
