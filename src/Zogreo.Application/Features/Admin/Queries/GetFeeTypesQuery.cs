using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;

namespace Zogreo.Application.Features.Admin.Queries;

public record GetFeeTypesQuery : IQuery<List<FeeTypeDto>>;

public class GetFeeTypesQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetFeeTypesQuery, List<FeeTypeDto>>
{
    public async Task<List<FeeTypeDto>> Handle(GetFeeTypesQuery q, CancellationToken ct)
        => await db.FeeTypes.AsNoTracking()
            .Select(f => new FeeTypeDto(f.Id, f.Code.ToString(), f.Name, f.Amount, f.Refundable, f.Active))
            .ToListAsync(ct);
}
