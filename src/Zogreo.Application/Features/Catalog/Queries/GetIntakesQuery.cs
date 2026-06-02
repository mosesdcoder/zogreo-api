using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Catalog.DTOs;

namespace Zogreo.Application.Features.Catalog.Queries;

public record GetIntakesQuery(Guid? ProgramId) : IQuery<List<IntakeDto>>;

public class GetIntakesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetIntakesQuery, List<IntakeDto>>
{
    public async Task<List<IntakeDto>> Handle(GetIntakesQuery q, CancellationToken ct)
    {
        var query = db.Intakes.AsNoTracking().Where(i => i.Active);
        if (q.ProgramId.HasValue) query = query.Where(i => i.ProgramId == q.ProgramId.Value);
        return await query
            .Select(i => new IntakeDto(i.Id, i.ProgramId, i.Name, i.OpensAt, i.ClosesAt, i.StartsAt, i.Capacity))
            .ToListAsync(ct);
    }
}
