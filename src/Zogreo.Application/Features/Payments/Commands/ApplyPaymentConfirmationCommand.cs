using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Payments.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Payments.Commands;

public record ApplyPaymentConfirmationCommand(string Reference, string? RawPayload = null)
    : ICommand<PaymentStatusDto?>;

public class ApplyPaymentConfirmationCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IPaystackClient paystack,
    INotificationOutbox outbox,
    IMoodleProvisioningTrigger moodleTrigger) : ICommandHandler<ApplyPaymentConfirmationCommand, PaymentStatusDto?>
{
    public async Task<PaymentStatusDto?> Handle(ApplyPaymentConfirmationCommand cmd, CancellationToken ct)
    {
        var payment = await db.Payments.IgnoreQueryFilters()
            .Include(p => p.Invoice).ThenInclude(i => i.Application).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(p => p.Reference == cmd.Reference, ct);

        if (payment == null) return null;

        // Idempotency guard
        if (payment.Status == PaymentStatus.Success)
            return ToDto(payment);

        // Resolve tenant from the payment's org (webhook bypasses TenantMiddleware)
        tenant.SetTenant(payment.OrganizationId, tenant.UserId, tenant.UserRole);

        var verified = await paystack.VerifyTransactionAsync(cmd.Reference);
        if (verified == null || verified.GatewayResponse.ToLower() != "successful")
            return ToDto(payment);

        var gross = verified.Amount / 100m;
        var providerFee = verified.Fees / 100m;
        var isTech = payment.Invoice.FeeCode == FeeCode.Technology;

        payment.Status = PaymentStatus.Success;
        payment.AmountGross = gross;
        payment.ProviderFee = providerFee;
        payment.TechnologyFee = isTech ? gross : 0m;
        payment.AmountNetToSchool = isTech ? 0m : gross - providerFee;
        payment.ProviderRef = verified.PaystackRef;
        payment.CompletedAt = DateTimeOffset.UtcNow;
        if (cmd.RawPayload != null) payment.RawPayload = cmd.RawPayload;

        var invoice = payment.Invoice;
        invoice.AmountPaid += gross;
        invoice.Status = invoice.AmountPaid >= invoice.Amount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        await db.SaveChangesAsync(ct);

        await AdvanceApplicationStateAsync(invoice, ct);

        var user = payment.Invoice.Application.User;
        await outbox.QueueEmailAsync(user.Id, user.Email, "payment_receipt",
            "Payment Received",
            $"Hi {user.FullName}, we received your {invoice.FeeCode} payment of KES {gross:N2}.");

        return ToDto(payment);
    }

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
            .Where(i => i.ApplicationId == app.Id && (i.FeeCode == FeeCode.Admission || i.FeeCode == FeeCode.Technology))
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
            .AnyAsync(d => d.ApplicationId == app.Id && d.Type == DocumentType.MedicalReport && d.Status == DocumentStatus.Verified, ct);

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
            OrganizationId = app.OrganizationId,
            ApplicationId = app.Id,
            UserId = app.UserId,
            AdmissionNumber = admNum,
            Status = StudentStatus.Active,
            EnrolledAt = DateTimeOffset.UtcNow
        });
        app.Enrol();
        await db.SaveChangesAsync(ct);

        // Provision the student in Moodle (fire-and-forget via background trigger)
        var student = await db.Students.IgnoreQueryFilters().FirstAsync(s => s.ApplicationId == app.Id, ct);
        await moodleTrigger.TriggerAsync(student.Id, ct);

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == app.UserId, ct);
        if (user != null)
            await outbox.QueueEmailAsync(user.Id, user.Email, "enrolled",
                "Enrolment Confirmed", $"Hi {user.FullName}, your admission number is {admNum}.");
    }

    private static PaymentStatusDto ToDto(Payment p) =>
        new(p.Id, p.Reference, p.Status.ToString(), p.AmountGross, p.ProviderFee, p.TechnologyFee, p.AmountNetToSchool, p.CompletedAt);
}
