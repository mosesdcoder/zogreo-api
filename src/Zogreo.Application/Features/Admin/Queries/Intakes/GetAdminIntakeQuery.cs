using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries.Intakes;

public record GetAdminIntakeQuery(Guid Id) : IQuery<IntakeDto>;

public class GetAdminIntakeQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetAdminIntakeQuery, IntakeDto>
{
    public async Task<IntakeDto> Handle(GetAdminIntakeQuery q, CancellationToken ct)
        => await db.Intakes.AsNoTracking().Include(i => i.Program)
            .Where(i => i.Id == q.Id)
            .Select(i => new IntakeDto(i.Id, i.ProgramId, i.Program.Name, i.Name, i.OpensAt, i.ClosesAt, i.StartsAt, i.Capacity, i.Active))
            .FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Intake not found.");
}
