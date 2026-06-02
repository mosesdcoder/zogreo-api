using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetStudentQuery(Guid Id) : IQuery<AdminStudentItem>;

public class GetStudentQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetStudentQuery, AdminStudentItem>
{
    public async Task<AdminStudentItem> Handle(GetStudentQuery q, CancellationToken ct)
        => await db.Students.AsNoTracking()
            .Include(s => s.User).Include(s => s.Application).ThenInclude(a => a.Program)
            .Where(s => s.Id == q.Id)
            .Select(s => new AdminStudentItem(s.Id, s.AdmissionNumber, s.User.FullName, s.User.Email, s.Application.Program.Name, s.Status.ToString(), s.EnrolledAt))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Student not found.");
}
