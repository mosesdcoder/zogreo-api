using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Offers.Commands;

public record AcceptOfferCommand(Guid ApplicationId) : ICommand<Unit>;

public class AcceptOfferCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant) : ICommandHandler<AcceptOfferCommand, Unit>
{
    public async Task<Unit> Handle(AcceptOfferCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");
        if (app.UserId != userId) throw AppException.Forbidden();
        if (app.Status != ApplicationStatus.OfferMade)
            throw new AppException("Offer is not in a state that can be accepted.", 422);

        var acceptInvoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.ApplicationId == cmd.ApplicationId && i.FeeCode == FeeCode.Acceptance, ct)
            ?? throw new AppException("Acceptance invoice not found.", 500);
        if (acceptInvoice.Status != InvoiceStatus.Paid)
            throw new AppException("Please pay the acceptance fee before accepting the offer.", 402);

        var offer = await db.Offers.FirstOrDefaultAsync(o => o.ApplicationId == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Offer not found.");

        offer.Status = OfferStatus.Accepted;
        offer.AcceptedAt = DateTimeOffset.UtcNow;
        app.AcceptOffer();
        await db.SaveChangesAsync(ct);

        return new Unit();
    }
}
