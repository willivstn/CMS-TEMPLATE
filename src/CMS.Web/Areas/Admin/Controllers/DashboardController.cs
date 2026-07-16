using CMS.Application.Common.Abstractions;
using CMS.Domain.Authorization;
using CMS.Domain.Entities.Identity;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>
/// The admin dashboard landing page. Thin: it composes read-only counts and
/// recent activity for display. Richer widgets (charts, live SignalR tiles) are
/// layered on in later phases.
/// </summary>
[Area("Admin")]
[HasPermission(Permissions.Dashboard.View)]
public sealed class DashboardController(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var recentUsers = await userManager.Users
            .OrderByDescending(u => u.CreatedOnUtc)
            .Take(5)
            .Select(u => new { u.FirstName, u.LastName, u.UserName, u.Email, u.CreatedOnUtc })
            .ToListAsync(cancellationToken);

        var model = new DashboardViewModel
        {
            UserCount = await userManager.Users.CountAsync(cancellationToken),
            RoleCount = await roleManager.Roles.CountAsync(cancellationToken),
            ContentCount = await db.ContentItems.CountAsync(cancellationToken),
            MediaCount = await db.MediaItems.CountAsync(cancellationToken),
            RecentActivity = await db.AuditLogs
                .OrderByDescending(a => a.TimestampUtc)
                .Take(8)
                .Select(a => new ActivityRow(a.Action, a.EntityName, a.UserName, a.TimestampUtc))
                .ToListAsync(cancellationToken),
            RecentUsers = recentUsers
                .Select(u => new RecentUserRow(
                    string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName)
                        ? u.UserName ?? u.Email ?? "—"
                        : $"{u.FirstName} {u.LastName}".Trim(),
                    u.Email,
                    u.CreatedOnUtc))
                .ToList(),
        };

        return View(model);
    }
}
