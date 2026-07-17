using CMS.Application.Features.FeatureFlags;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Feature-flag management (create, toggle, delete).</summary>
[Area("Admin")]
[HasPermission(Permissions.System.ManageFeatureFlags)]
public sealed class FeatureFlagsController(ISender mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetFeatureFlagsQuery(), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, string? environment, bool isEnabled, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateFeatureFlagCommand(name ?? string.Empty, description, environment, isEnabled), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Flag created." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id, bool isEnabled, CancellationToken ct)
    {
        await mediator.Send(new ToggleFeatureFlagCommand(id, isEnabled), ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteFeatureFlagCommand(id), ct);
        TempData["flash:success"] = "Flag deleted.";
        return RedirectToAction(nameof(Index));
    }
}
