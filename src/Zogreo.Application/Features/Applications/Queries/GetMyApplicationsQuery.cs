using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Applications.DTOs;
using Zogreo.Application.Features.Applications.Mappings;

namespace Zogreo.Application.Features.Applications.Queries;

public record GetMyApplicationsQuery : IQuery<List<ApplicationSummaryDto>>;

public class GetMyApplicationsQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetMyApplicationsQuery, List<ApplicationSummaryDto>>
{
    public async Task<List<ApplicationSummaryDto>> Handle(GetMyApplicationsQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var apps = await db.Applications.AsNoTracking()
            .Include(a => a.Program).Include(a => a.Intake)
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        return apps.Select(a => ApplicationMapper.ToSummary(a, a.Program.Name, a.Intake.Name)).ToList();
    }
}
