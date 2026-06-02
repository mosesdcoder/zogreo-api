using Zogreo.Application.Common.Interfaces;
using Zogreo.Domain.Entities;
using Zogreo.Domain.Enums;

namespace Zogreo.Infrastructure.Notifications;

public class NotificationOutbox(IApplicationDbContext db, ITenantProvider tenant) : INotificationOutbox
{
    public async Task QueueSmsAsync(Guid? userId, string phone, string template, string body)
    {
        db.Notifications.Add(new Notification
        {
            OrganizationId = tenant.OrganizationId,
            UserId = userId,
            Channel = NotificationChannel.Sms,
            To = phone,
            Template = template,
            Body = body,
            Status = NotificationStatus.Queued
        });
        await db.SaveChangesAsync();
    }

    public async Task QueueEmailAsync(Guid? userId, string email, string template, string subject, string body)
    {
        db.Notifications.Add(new Notification
        {
            OrganizationId = tenant.OrganizationId,
            UserId = userId,
            Channel = NotificationChannel.Email,
            To = email,
            Template = template,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Queued
        });
        await db.SaveChangesAsync();
    }
}
