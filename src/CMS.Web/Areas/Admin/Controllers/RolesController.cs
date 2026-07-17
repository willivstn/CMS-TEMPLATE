using CMS.Application.Features.Permissions;
using CMS.Application.Features.Roles;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Perms = CMS.Domain.Authorization.Permissions;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Role administration and the role permission matrix.</summary>
[Area("Admin")]
public sealed class RolesController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Perms.Roles.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetRolesQuery(), ct));

    [HttpGet]
    [HasPermission(Perms.Roles.Create)]
    public IActionResult Create() => View(new RoleFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Roles.Create)]
    public async Task<IActionResult> Create(RoleFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await mediator.Send(new CreateRoleCommand(model.Name, model.Description), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Role created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Perms.Roles.Edit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var role = await mediator.Send(new GetRoleForEditQuery(id), ct);
        return role is null
            ? NotFound()
            : View(new RoleFormViewModel { Id = role.Id, Name = role.Name, Description = role.Description, IsSystem = role.IsSystem });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Roles.Edit)]
    public async Task<IActionResult> Edit(RoleFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var result = await mediator.Send(new UpdateRoleCommand(model.Id.Value, model.Name, model.Description), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Role updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Roles.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteRoleCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] =
            result.IsSuccess ? "Role deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Perms.PermissionsAdmin.Assign)]
    public async Task<IActionResult> Permissions(Guid id, CancellationToken ct)
    {
        var role = await mediator.Send(new GetRolePermissionsQuery(id), ct);
        if (role is null)
        {
            return NotFound();
        }

        var model = new RolePermissionsViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            IsSystem = role.IsSystem,
            Groups = await mediator.Send(new GetPermissionCatalogQuery(), ct),
            Selected = [.. role.SelectedPermissions],
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.PermissionsAdmin.Assign)]
    public async Task<IActionResult> Permissions(Guid id, [FromForm] List<string> permissions, CancellationToken ct)
    {
        var result = await mediator.Send(new SetRolePermissionsCommand(id, permissions), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] =
            result.IsSuccess ? "Role permissions saved." : result.Error.Message;
        return RedirectToAction(nameof(Permissions), new { id });
    }
}
