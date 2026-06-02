using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record ReviewDocumentCommand(Guid DocumentId, string Status, string? Reason) : ICommand<Unit>;

public class ReviewDocumentCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    INotificationOutbox outbox) : ICommandHandler<ReviewDocumentCommand, Unit>
{
    public async Task<Unit> Handle(ReviewDocumentCommand cmd, CancellationToken ct)
    {
        var adminId = tenant.UserId ?? throw AppException.Unauthorized();

        if (!Enum.TryParse<DocumentStatus>(cmd.Status, true, out var status))
            throw new AppException("Invalid document status.", 422);

        var doc = await db.Documents
            .Include(d => d.Application).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, ct)
            ?? throw AppException.NotFound("Document not found.");

        doc.Status = status;
        doc.ReviewReason = cmd.Reason;
        doc.ReviewedByUserId = adminId;
        doc.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (status is DocumentStatus.Rejected or DocumentStatus.NeedsResubmission)
        {
            var user = doc.Application.User;
            await outbox.QueueSmsAsync(user.Id, user.Phone, "doc_rejected",
                $"Your {doc.Type} document needs attention. Reason: {cmd.Reason}");
            await outbox.QueueEmailAsync(user.Id, user.Email, "doc_rejected",
                "Document Update Required",
                $"Hi {user.FullName}, your {doc.Type} document was rejected. Reason: {cmd.Reason}. Please re-upload.");
        }

        await TryAdvanceDocsVerifiedAsync(doc.Application, ct);

        if (doc.Type == DocumentType.MedicalReport && status == DocumentStatus.Verified)
            await TryAdvanceMedicalsClearedAsync(doc.Application, ct);

        return new Unit();
    }

    private async Task TryAdvanceDocsVerifiedAsync(Domain.Entities.Application app, CancellationToken ct)
    {
        if (app.Status != ApplicationStatus.UnderReview) return;
        var required = new[] { DocumentType.NationalIdOrPassport, DocumentType.AcademicCertificate, DocumentType.PassportPhoto };
        var docs = await db.Documents.Where(d => d.ApplicationId == app.Id).ToListAsync(ct);
        if (required.All(t => docs.Any(d => d.Type == t && d.Status == DocumentStatus.Verified)))
        {
            app.MarkDocsVerified();
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task TryAdvanceMedicalsClearedAsync(Domain.Entities.Application app, CancellationToken ct)
    {
        var fresh = await db.Applications.FirstOrDefaultAsync(a => a.Id == app.Id, ct);
        if (fresh?.Status != ApplicationStatus.FeesPaid) return;
        var medInv = await db.Invoices.FirstOrDefaultAsync(i => i.ApplicationId == app.Id && i.FeeCode == FeeCode.Medicals, ct);
        if (medInv?.Status == InvoiceStatus.Paid)
        {
            fresh.ClearMedicals();
            await db.SaveChangesAsync(ct);
        }
    }
}
