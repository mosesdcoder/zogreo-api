using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries.Intakes;

public record GetAdminIntakesQuery(Guid? ProgramId, bool IncludeInactive = false) : IQuery<List<IntakeDto>>;

public class GetAdminIntakesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAdminIntakesQuery, List<IntakeDto>>
{
    public async Task<List<IntakeDto>> Handle(GetAdminIntakesQuery q, CancellationToken ct)
    {
        var query = db.Intakes.AsNoTracking().Include(i => i.Program).AsQueryable();
        if (q.ProgramId.HasValue) query = query.Where(i => i.ProgramId == q.ProgramId.Value);
        if (!q.IncludeInactive) query = query.Where(i => i.Active);
        return await query
            .Select(i => new IntakeDto(i.Id, i.ProgramId, i.Program.Name, i.Name, i.OpensAt, i.ClosesAt, i.StartsAt, i.Capacity, i.Active))
            .ToListAsync(ct);
    }
}
