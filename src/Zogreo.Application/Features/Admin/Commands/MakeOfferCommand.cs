using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record MakeOfferCommand(Guid ApplicationId, string? Conditions, int ExpiryDays) : ICommand<Unit>;

public class MakeOfferCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IOfferLetterGenerator letterGen,
    INotificationOutbox outbox) : ICommandHandler<MakeOfferCommand, Unit>
{
    public async Task<Unit> Handle(MakeOfferCommand cmd, CancellationToken ct)
    {
        var adminId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications
            .Include(a => a.User).Include(a => a.Program).Include(a => a.Intake).Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddDays(cmd.ExpiryDays > 0 ? cmd.ExpiryDays : 14);

        var offer = new Offer
        {
            OrganizationId = tenant.OrganizationId,
            ApplicationId = app.Id,
            Status = OfferStatus.Issued,
            Conditions = cmd.Conditions,
            IssuedAt = now,
            ExpiresAt = expiry
        };
        db.Offers.Add(offer);

        foreach (var code in new[] { FeeCode.Acceptance, FeeCode.Admission, FeeCode.Medicals, FeeCode.Technology })
        {
            if (!app.Invoices.Any(i => i.FeeCode == code))
            {
                var ft = await db.FeeTypes.FirstOrDefaultAsync(f => f.Code == code, ct)
                    ?? throw new AppException($"Fee type {code} not configured.", 500);
                db.Invoices.Add(new Invoice
                {
                    OrganizationId = tenant.OrganizationId,
                    ApplicationId = app.Id,
                    FeeTypeId = ft.Id,
                    FeeCode = code,
                    Amount = ft.Amount
                });
            }
        }

        app.MakeOffer(adminId);
        await db.SaveChangesAsync(ct);

        await db.Applications.Entry(app).Collection(a => a.Invoices).LoadAsync(ct);
        offer.LetterUrl = await letterGen.GenerateAsync(app, offer, ct);
        await db.SaveChangesAsync(ct);

        var user = app.User;
        await outbox.QueueSmsAsync(user.Id, user.Phone, "offer_made",
            $"Congratulations {user.FullName}! Offer for {app.Program.Name}. Expires {expiry:d MMM yyyy}.");
        await outbox.QueueEmailAsync(user.Id, user.Email, "offer_made",
            "Admission Offer",
            $"Hi {user.FullName}, you have been offered admission to {app.Program.Name}. Accept by {expiry:d MMM yyyy}.");

        return new Unit();
    }
}
