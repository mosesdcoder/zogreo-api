using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Document : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public DocumentType Type { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }

    public Application Application { get; set; } = null!;
}
