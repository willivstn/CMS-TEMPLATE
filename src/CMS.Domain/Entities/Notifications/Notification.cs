using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Notifications;

/// <summary>
/// A notification delivered in-app (and pushed live over SignalR). May target
/// specific users via <see cref="Recipients"/> or be a broadcast to everyone.
/// </summary>
public class Notification : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>Optional deep link the notification navigates to when clicked.</summary>
    public string? Link { get; set; }

    /// <summary>Optional icon CSS class.</summary>
    public string? Icon { get; set; }

    /// <summary>When true, delivered to all users rather than named recipients.</summary>
    public bool IsBroadcast { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = [];
}
