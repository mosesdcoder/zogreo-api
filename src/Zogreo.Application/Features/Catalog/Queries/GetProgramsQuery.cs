using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Catalog.DTOs;

namespace Zogreo.Application.Features.Catalog.Queries;

public record GetProgramsQuery : IQuery<List<ProgramDto>>;

public class GetProgramsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetProgramsQuery, List<ProgramDto>>
{
    public async Task<List<ProgramDto>> Handle(GetProgramsQuery q, CancellationToken ct)
        => await db.Programs.AsNoTracking()
            .Where(p => p.Active)
            .Select(p => new ProgramDto(p.Id, p.Name, p.Level.ToString(), p.Mode.ToString(), p.DurationLabel, p.Description))
            .ToListAsync(ct);
}
