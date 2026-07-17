using CMS.Application.Common.Abstractions;
using CMS.Domain.Common;
using CMS.Domain.Entities.Notifications;
using CMS.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Web.Realtime;

/// <summary>
/// Persists notifications and pushes them live over SignalR. Per-user
/// notifications create one recipient row; broadcasts fan out to every user and
/// push to all connected clients.
/// </summary>
public sealed class NotificationService(
    IApplicationDbContext db,
    IHubContext<NotificationHub> hub) : INotificationService
{
    public async Task NotifyUserAsync(
        Guid userId, string title, string message, NotificationType type = NotificationType.Info,
        string? link = null, CancellationToken cancellationToken = default)
    {
        var notification = NewNotification(title, message, type, link, isBroadcast: false);
        notification.Recipients.Add(new NotificationRecipient { Id = Guid.NewGuid(), UserId = userId });

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        await hub.Clients.User(userId.ToString())
            .SendAsync(NotificationHub.ReceiveNotification, Payload(notification), cancellationToken);
    }

    public async Task BroadcastAsync(
        string title, string message, NotificationType type = NotificationType.Info,
        string? link = null, CancellationToken cancellationToken = default)
    {
        var notification = NewNotification(title, message, type, link, isBroadcast: true);

        var userIds = await db.Users.Select(u => u.Id).ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            notification.Recipients.Add(new NotificationRecipient { Id = Guid.NewGuid(), UserId = userId });
        }

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        await hub.Clients.All.SendAsync(NotificationHub.ReceiveNotification, Payload(notification), cancellationToken);
    }

    private static Notification NewNotification(string title, string message, NotificationType type, string? link, bool isBroadcast) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = DomainDefaults.DefaultTenantId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            IsBroadcast = isBroadcast,
        };

    private static object Payload(Notification n) => new
    {
        id = n.Id,
        title = n.Title,
        message = n.Message,
        type = n.Type.ToString().ToLowerInvariant(),
        link = n.Link,
    };
}
