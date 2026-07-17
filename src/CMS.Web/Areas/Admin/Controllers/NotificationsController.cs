using CMS.Application.Common.Abstractions;
using CMS.Application.Features.Notifications;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin broadcast composer plus the per-user notification-bell endpoints
/// (recent list, unread count, mark-read) consumed by the layout via fetch.
/// </summary>
[Area("Admin")]
[Authorize]
public sealed class NotificationsController(ISender mediator, INotificationService notifications, ICurrentUser currentUser) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Notifications.Send)]
    public IActionResult Index() => View(new NotificationBroadcastViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Notifications.Send)]
    public async Task<IActionResult> Send(NotificationBroadcastViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        await notifications.BroadcastAsync(model.Title, model.Message, model.Type, model.Link, ct);
        TempData["flash:success"] = "Notification broadcast to all users.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Notification bell (any authenticated user) ----

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var items = await mediator.Send(new GetUserNotificationsQuery(userId, 15), ct);
        var unread = await mediator.Send(new GetUnreadNotificationCountQuery(userId), ct);
        return Json(new { unread, items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        await mediator.Send(new MarkNotificationReadCommand(userId, id), ct);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        await mediator.Send(new MarkAllNotificationsReadCommand(userId), ct);
        return Ok();
    }
}
