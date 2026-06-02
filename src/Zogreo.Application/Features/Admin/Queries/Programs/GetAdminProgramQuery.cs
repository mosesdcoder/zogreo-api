using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries.Programs;

public record GetAdminProgramQuery(Guid Id) : IQuery<ProgramDto>;

public class GetAdminProgramQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAdminProgramQuery, ProgramDto>
{
    public async Task<ProgramDto> Handle(GetAdminProgramQuery q, CancellationToken ct)
        => await db.Programs.AsNoTracking()
            .Where(p => p.Id == q.Id)
            .Select(p => new ProgramDto(p.Id, p.Name, p.Level.ToString(), p.Mode.ToString(), p.DurationLabel, p.Description, p.Active))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Program not found.");
}
