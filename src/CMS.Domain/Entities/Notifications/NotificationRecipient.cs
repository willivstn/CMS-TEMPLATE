using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;

namespace CMS.Domain.Entities.Notifications;

/// <summary>
/// Per-user delivery and read state for a <see cref="Notification"/>. A broadcast
/// notification fans out into one recipient row per user (or is matched at read
/// time, depending on volume).
/// </summary>
public class NotificationRecipient : Entity<Guid>
{
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadOnUtc { get; set; }
}
