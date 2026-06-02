using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Enums;

namespace Zogreo.Infrastructure.Notifications;

public class NotificationDispatchJob(
    IApplicationDbContext db, ISmsSender sms, IEmailSender email,
    ILogger<NotificationDispatchJob> logger)
{
    public async Task RunAsync()
    {
        var queued = await db.Notifications.IgnoreQueryFilters()
            .Where(n => n.Status == NotificationStatus.Queued)
            .Take(50).ToListAsync();

        foreach (var n in queued)
        {
            try
            {
                if (n.Channel == NotificationChannel.Sms)
                    await sms.SendAsync(n.To, n.Body);
                else
                    await email.SendAsync(n.To, n.Subject ?? "Zogreo", n.Body);
                n.Status = NotificationStatus.Sent;
                n.SentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dispatch failed for notification {Id}", n.Id);
                n.Status = NotificationStatus.Failed;
                n.Error = ex.Message;
            }
        }

        if (queued.Count > 0) await db.SaveChangesAsync();
    }
}
