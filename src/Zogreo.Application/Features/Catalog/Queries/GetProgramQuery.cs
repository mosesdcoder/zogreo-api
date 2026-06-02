using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Catalog.DTOs;

namespace Zogreo.Application.Features.Catalog.Queries;

public record GetProgramQuery(Guid Id) : IQuery<ProgramDto>;

public class GetProgramQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetProgramQuery, ProgramDto>
{
    public async Task<ProgramDto> Handle(GetProgramQuery q, CancellationToken ct)
        => await db.Programs.AsNoTracking()
            .Where(p => p.Active && p.Id == q.Id)
            .Select(p => new ProgramDto(p.Id, p.Name, p.Level.ToString(), p.Mode.ToString(), p.DurationLabel, p.Description))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Program not found.");
}
