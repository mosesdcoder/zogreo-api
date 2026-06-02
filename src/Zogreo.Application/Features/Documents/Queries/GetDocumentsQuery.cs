using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Documents.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Documents.Queries;

public record GetDocumentsQuery(Guid ApplicationId) : IQuery<List<DocumentDto>>;

public class GetDocumentsQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetDocumentsQuery, List<DocumentDto>>
{
    public async Task<List<DocumentDto>> Handle(GetDocumentsQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == q.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (tenant.UserRole == Role.Applicant.ToString() && app.UserId != userId)
            throw AppException.Forbidden();

        return await db.Documents.AsNoTracking()
            .Where(d => d.ApplicationId == q.ApplicationId)
            .OrderBy(d => d.Type)
            .Select(d => new DocumentDto(d.Id, d.Type.ToString(), d.FileUrl, d.OriginalFileName, d.Status.ToString(), d.ReviewReason, d.CreatedAt))
            .ToListAsync(ct);
    }
}
