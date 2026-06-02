using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries.Programs;

public record GetAdminProgramsQuery(bool IncludeInactive = false) : IQuery<List<ProgramDto>>;

public class GetAdminProgramsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAdminProgramsQuery, List<ProgramDto>>
{
    public async Task<List<ProgramDto>> Handle(GetAdminProgramsQuery q, CancellationToken ct)
    {
        var query = db.Programs.AsNoTracking().AsQueryable();
        if (!q.IncludeInactive) query = query.Where(p => p.Active);
        return await query
            .Select(p => new ProgramDto(p.Id, p.Name, p.Level.ToString(), p.Mode.ToString(), p.DurationLabel, p.Description, p.Active))
            .ToListAsync(ct);
    }
}
