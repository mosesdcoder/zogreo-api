using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Offers.DTOs;

namespace Zogreo.Application.Features.Offers.Queries;

public record GetOfferQuery(Guid ApplicationId) : IQuery<OfferDto>;

public class GetOfferQueryHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : IQueryHandler<GetOfferQuery, OfferDto>
{
    public async Task<OfferDto> Handle(GetOfferQuery q, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == q.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");
        if (app.UserId != userId) throw AppException.Forbidden();

        var offer = await db.Offers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ApplicationId == q.ApplicationId, ct)
            ?? throw AppException.NotFound("No offer found for this application.");

        return new OfferDto(offer.Id, offer.Status.ToString(), offer.LetterUrl, offer.Conditions, offer.IssuedAt, offer.ExpiresAt, offer.AcceptedAt);
    }
}
