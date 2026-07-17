using CMS.Domain.Enums;

namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Creates notifications (persisted) and pushes them live over SignalR. Delivery
/// targets a single user or broadcasts to everyone.
/// </summary>
public interface INotificationService
{
    /// <summary>Notifies a single user.</summary>
    Task NotifyUserAsync(Guid userId, string title, string message, NotificationType type = NotificationType.Info, string? link = null, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a notification to all users.</summary>
    Task BroadcastAsync(string title, string message, NotificationType type = NotificationType.Info, string? link = null, CancellationToken cancellationToken = default);
}
