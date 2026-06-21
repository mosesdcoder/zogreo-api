using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Payments.Commands;

/// <summary>
/// Dev-only: creates a Payment row and immediately marks it Successful in one step.
/// Bypasses Paystack entirely — no initiate, no verify round-trip.
/// Advances the application state machine identically to a real confirmed payment.
/// </summary>
public record SimulateInvoicePayCommand(Guid InvoiceId) : ICommand<PaymentStatusDto>;

public class SimulateInvoicePayCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    INotificationOutbox outbox,
    IMoodleProvisioningTrigger moodleTrigger) : ICommandHandler<SimulateInvoicePayCommand, PaymentStatusDto>
{
    public async Task<PaymentStatusDto> Handle(SimulateInvoicePayCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var invoice = await db.Invoices
            .Include(i => i.Application).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct)
            ?? throw AppException.NotFound("Invoice not found.");

        if (invoice.Application.UserId != userId && tenant.UserRole == Role.Applicant.ToString())
            throw AppException.Forbidden();

        if (invoice.Status == InvoiceStatus.Paid)
            throw AppException.Conflict("Invoice is already paid.");

        var nonce     = Guid.NewGuid().ToString("N")[..8];
        var reference = $"{tenant.OrganizationId}:{invoice.Id}:{nonce}";
        var gross     = invoice.Amount;
        var isTech    = invoice.FeeCode == FeeCode.Technology;

        var payment = new Payment
        {
            OrganizationId    = tenant.OrganizationId,
            InvoiceId         = invoice.Id,
            Reference         = reference,
            Channel           = PaymentChannel.Other,
            Status            = PaymentStatus.Success,
            AmountGross       = gross,
            ProviderFee       = 0m,
            TechnologyFee     = isTech ? gross : 0m,
            AmountNetToSchool = isTech ? 0m : gross,
            ProviderRef       = "SIM-" + nonce,
            RawPayload        = $"{{\"simulated\":true,\"reference\":\"{reference}\"}}",
            CompletedAt       = DateTimeOffset.UtcNow,
        };
        db.Payments.Add(payment);

        invoice.AmountPaid += gross;
        invoice.Status = invoice.AmountPaid >= invoice.Amount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);

        await AdvanceApplicationStateAsync(invoice, ct);

        var user = invoice.Application.User;
        await outbox.QueueEmailAsync(user.Id, user.Email, "payment_receipt",
            "Payment Received (Simulated)",
            $"Hi {user.FullName}, simulation credited your {invoice.FeeCode} payment of KES {gross:N2}.");

        return new PaymentStatusDto(payment.Id, reference, "Success", gross, 0m, payment.TechnologyFee, payment.AmountNetToSchool, payment.CompletedAt);
    }

    // ── State machine (identical to SimulatePaymentCommand / ApplyPaymentConfirmationCommand) ──

    private async Task AdvanceApplicationStateAsync(Invoice invoice, CancellationToken ct)
    {
        var app = await db.Applications.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == invoice.ApplicationId, ct);
        if (app == null) return;

        switch (invoice.FeeCode)
        {
            case FeeCode.Application when app.Status == ApplicationStatus.Draft:
                app.Submit();
                await db.SaveChangesAsync(ct);
                break;

            case FeeCode.Acceptance when app.Status == ApplicationStatus.OfferMade:
                app.AcceptOffer();
                var offer = await db.Offers.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.ApplicationId == app.Id, ct);
                if (offer != null) { offer.Status = OfferStatus.Accepted; offer.AcceptedAt = DateTimeOffset.UtcNow; }
                await db.SaveChangesAsync(ct);
                break;

            case FeeCode.Admission or FeeCode.Technology:
                await TryAdvanceToFeesPaidAsync(app, ct);
                break;

            case FeeCode.Medicals:
                await TryAdvanceMedicalsClearedAsync(app, ct);
                break;
        }
    }

    private async Task TryAdvanceToFeesPaidAsync(Domain.Entities.Application app, CancellationToken ct)
    {
        if (app.Status != ApplicationStatus.OfferAccepted) return;
        var invoices = await db.Invoices.IgnoreQueryFilters()
            .Where(i => i.ApplicationId == app.Id &&
                        (i.FeeCode == FeeCode.Admission || i.FeeCode == FeeCode.Technology))
            .ToListAsync(ct);
        if (invoices.Count == 2 && invoices.All(i => i.Status == InvoiceStatus.Paid))
        {
            app.MarkFeesPaid();
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task TryAdvanceMedicalsClearedAsync(Domain.Entities.Application app, CancellationToken ct)
    {
        if (app.Status != ApplicationStatus.FeesPaid) return;
        var medInvoice = await db.Invoices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.ApplicationId == app.Id && i.FeeCode == FeeCode.Medicals, ct);
        var medDocVerified = await db.Documents.IgnoreQueryFilters()
            .AnyAsync(d => d.ApplicationId == app.Id &&
                           d.Type == DocumentType.MedicalReport &&
                           d.Status == DocumentStatus.Verified, ct);

        if (medInvoice?.Status == InvoiceStatus.Paid && medDocVerified)
        {
            app.ClearMedicals();
            await db.SaveChangesAsync(ct);
            await EnrolStudentAsync(app, ct);
        }
    }

    private async Task EnrolStudentAsync(Domain.Entities.Application app, CancellationToken ct)
    {
        if (await db.Students.IgnoreQueryFilters().AnyAsync(s => s.ApplicationId == app.Id, ct)) return;

        var org = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Id == app.OrganizationId, ct);
        var year = DateTime.UtcNow.Year;
        var count = await db.Students.IgnoreQueryFilters()
            .CountAsync(s => s.OrganizationId == app.OrganizationId && s.EnrolledAt.Year == year, ct);
        var admNum = $"{org.AdmissionNumberPrefix}/{year % 100:D2}/{(count + 1):D4}";

        db.Students.Add(new Student
        {
            OrganizationId  = app.OrganizationId,
            ApplicationId   = app.Id,
            UserId          = app.UserId,
            AdmissionNumber = admNum,
            Status          = StudentStatus.Active,
            EnrolledAt      = DateTimeOffset.UtcNow
        });
        app.Enrol();
        await db.SaveChangesAsync(ct);

        var student = await db.Students.IgnoreQueryFilters().FirstAsync(s => s.ApplicationId == app.Id, ct);
        await moodleTrigger.TriggerAsync(student.Id, ct);

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == app.UserId, ct);
        if (user != null)
            await outbox.QueueEmailAsync(user.Id, user.Email, "enrolled",
                "Enrolment Confirmed", $"Hi {user.FullName}, your admission number is {admNum}.");
    }
}
