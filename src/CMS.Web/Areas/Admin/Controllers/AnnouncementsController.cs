using CMS.Application.Features.Announcements;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin UI for the sample Announcements module. Structurally identical to the
/// core controllers — proof that a module is just another vertical slice.
/// </summary>
[Area("Admin")]
public sealed class AnnouncementsController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Announcements.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetAnnouncementsQuery(), ct));

    [HttpGet]
    [HasPermission(Permissions.Announcements.Manage)]
    public IActionResult Create() => View(new AnnouncementFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Announcements.Manage)]
    public async Task<IActionResult> Create(AnnouncementFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await mediator.Send(new CreateAnnouncementCommand(model.Title, model.Body, model.IsPublished), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Announcement created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Permissions.Announcements.Manage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var a = await mediator.Send(new GetAnnouncementForEditQuery(id), ct);
        return a is null
            ? NotFound()
            : View(new AnnouncementFormViewModel { Id = a.Id, Title = a.Title, Body = a.Body, IsPublished = a.IsPublished });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Announcements.Manage)]
    public async Task<IActionResult> Edit(AnnouncementFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var result = await mediator.Send(new UpdateAnnouncementCommand(model.Id.Value, model.Title, model.Body, model.IsPublished), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Announcement saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Announcements.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteAnnouncementCommand(id), ct);
        TempData["flash:success"] = "Announcement deleted.";
        return RedirectToAction(nameof(Index));
    }
}
