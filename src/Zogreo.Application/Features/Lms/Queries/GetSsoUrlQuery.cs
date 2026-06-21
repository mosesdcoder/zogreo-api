using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;

namespace Zogreo.Application.Features.Lms.Queries;

public record GetSsoUrlQuery(string ReturnUrl = "/my") : IQuery<string>;

public class GetSsoUrlQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IMoodleClient moodle) : IQueryHandler<GetSsoUrlQuery, string>
{
    public async Task<string> Handle(GetSsoUrlQuery query, CancellationToken ct)
    {
        var moodleUser = await db.MoodleUsers
            .FirstOrDefaultAsync(m => m.UserId == tenant.UserId, ct)
            ?? throw AppException.NotFound("LMS account not provisioned yet. Please contact the registrar.");

        return await moodle.GetAutoLoginUrlAsync(moodleUser.MoodleUsername, query.ReturnUrl, ct);
    }
}
