using CMS.Application.Features.Theming;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Theme engine: manage selectable UI themes (CSS-variable overrides).</summary>
[Area("Admin")]
public sealed class ThemesController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Settings.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetThemesQuery(), ct));

    [HttpGet]
    [HasPermission(Permissions.Settings.Manage)]
    public IActionResult Create() => View(new ThemeFormViewModel
    {
        Variables = "{\n  \"--bs-primary\": \"#6f42c1\"\n}",
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> Create(ThemeFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await mediator.Send(new CreateThemeCommand(model.Key, model.Name, model.IsDark, model.IsEnabled, model.Variables, model.SortOrder), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Theme created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var t = await mediator.Send(new GetThemeForEditQuery(id), ct);
        return t is null
            ? NotFound()
            : View(new ThemeFormViewModel { Id = t.Id, Key = t.Key, Name = t.Name, IsDark = t.IsDark, IsEnabled = t.IsEnabled, Variables = t.Variables, SortOrder = t.SortOrder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> Edit(ThemeFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var result = await mediator.Send(new UpdateThemeCommand(model.Id.Value, model.Name, model.IsDark, model.IsEnabled, model.Variables, model.SortOrder), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Theme saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteThemeCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Theme deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
