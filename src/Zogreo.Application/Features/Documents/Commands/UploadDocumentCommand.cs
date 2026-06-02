using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Documents.DTOs;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Documents.Commands;

public record UploadDocumentCommand(Guid ApplicationId, DocumentType Type, IFileProxy File)
    : ICommand<DocumentDto>;

public class UploadDocumentCommandHandler(
    IApplicationDbContext db,
    ITenantProvider tenant,
    IFileStorage storage) : ICommandHandler<UploadDocumentCommand, DocumentDto>
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private const long MaxBytes = 10 * 1024 * 1024;

    public async Task<DocumentDto> Handle(UploadDocumentCommand cmd, CancellationToken ct)
    {
        var userId = tenant.UserId ?? throw AppException.Unauthorized();

        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw AppException.NotFound("Application not found.");
        if (app.UserId != userId) throw AppException.Forbidden();

        if (!AllowedContentTypes.Contains(cmd.File.ContentType))
            throw new AppException("Only JPEG, PNG, and PDF files are allowed.", 422);
        if (cmd.File.Length > MaxBytes)
            throw new AppException("File must be under 10 MB.", 422);

        var url = await storage.SaveAsync(cmd.File, $"applications/{cmd.ApplicationId}", ct);

        var doc = new Document
        {
            OrganizationId = tenant.OrganizationId,
            ApplicationId = cmd.ApplicationId,
            Type = cmd.Type,
            FileUrl = url,
            OriginalFileName = cmd.File.FileName,
            Status = DocumentStatus.Pending
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.Type.ToString(), doc.FileUrl, doc.OriginalFileName, doc.Status.ToString(), doc.ReviewReason, doc.CreatedAt);
    }
}
