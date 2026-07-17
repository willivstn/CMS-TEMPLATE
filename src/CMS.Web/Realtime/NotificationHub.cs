using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CMS.Web.Realtime;

/// <summary>
/// SignalR hub for live notifications and dashboard updates. Clients are addressed
/// by their user id (SignalR's default <c>UserIdentifier</c> = the NameIdentifier
/// claim), so the server pushes with <c>Clients.User(id)</c> — no manual grouping
/// required. The hub itself is push-only; persistence and fan-out live in
/// <see cref="NotificationService"/>.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    /// <summary>The event name clients subscribe to for incoming notifications.</summary>
    public const string ReceiveNotification = "ReceiveNotification";
}
