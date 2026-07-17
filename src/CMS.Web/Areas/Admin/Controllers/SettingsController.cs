using CMS.Application.Features.Settings;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Grouped, typed application settings editor.</summary>
[Area("Admin")]
public sealed class SettingsController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Settings.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetSettingsQuery(), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> Index([FromForm] Dictionary<string, string?> values, CancellationToken ct)
    {
        await mediator.Send(new UpdateSettingsCommand(values), ct);
        TempData["flash:success"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
