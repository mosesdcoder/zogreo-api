using Zogreo.Domain.Common;
using Zogreo.Domain.Enums;

namespace Zogreo.Domain.Entities;

public class Notification : TenantEntity
{
    public Guid? UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string To { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public string? Error { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
