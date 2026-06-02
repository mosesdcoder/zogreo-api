using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetStudentsQuery(int Page, int PageSize = 20) : IQuery<PagedResult<AdminStudentItem>>;

public class GetStudentsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetStudentsQuery, PagedResult<AdminStudentItem>>
{
    public async Task<PagedResult<AdminStudentItem>> Handle(GetStudentsQuery q, CancellationToken ct)
    {
        var total = await db.Students.CountAsync(ct);
        var items = await db.Students.AsNoTracking()
            .Include(s => s.User).Include(s => s.Application).ThenInclude(a => a.Program)
            .OrderByDescending(s => s.EnrolledAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(s => new AdminStudentItem(s.Id, s.AdmissionNumber, s.User.FullName, s.User.Email, s.Application.Program.Name, s.Status.ToString(), s.EnrolledAt))
            .ToListAsync(ct);

        return new PagedResult<AdminStudentItem>(total, q.Page, items);
    }
}
