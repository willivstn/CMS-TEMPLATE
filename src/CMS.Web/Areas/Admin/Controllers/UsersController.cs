using CMS.Application.Features.Permissions;
using CMS.Application.Features.Users;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Perms = CMS.Domain.Authorization.Permissions;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>
/// User administration. Thin actions dispatch CQRS requests via MediatR and
/// translate <c>Result</c> outcomes to redirects/flash messages or re-rendered
/// forms. Each action is guarded by a specific permission.
/// </summary>
[Area("Admin")]
public sealed class UsersController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Perms.Users.View)]
    public async Task<IActionResult> Index(string? search, string? role, bool? active, int page = 1, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(search, role, active, page), ct);
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.Active = active;
        ViewBag.Roles = await mediator.Send(new GetRoleOptionsQuery(), ct);
        return View(result);
    }

    [HttpGet]
    [HasPermission(Perms.Users.View)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserDetailsQuery(id), ct);
        return user is null ? NotFound() : View(user);
    }

    [HttpGet]
    [HasPermission(Perms.Users.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new UserCreateViewModel { AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct) };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.Create)]
    public async Task<IActionResult> Create(UserCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct);
            return View(model);
        }

        var result = await mediator.Send(
            new CreateUserCommand(model.Email, model.FirstName, model.LastName, model.Password, model.IsActive, model.SelectedRoles), ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct);
            return View(model);
        }

        TempData["flash:success"] = "User created.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    [HasPermission(Perms.Users.Edit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserForEditQuery(id), ct);
        if (user is null)
        {
            return NotFound();
        }

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            SelectedRoles = user.SelectedRoles.ToList(),
            AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.Edit)]
    public async Task<IActionResult> Edit(UserEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct);
            return View(model);
        }

        var result = await mediator.Send(
            new UpdateUserCommand(model.Id, model.FirstName, model.LastName, model.IsActive, model.SelectedRoles), ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.AllRoles = await mediator.Send(new GetRoleOptionsQuery(), ct);
            return View(model);
        }

        TempData["flash:success"] = "User updated.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.Edit)]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct)
    {
        await FlashResult(await mediator.Send(new SetUserLockoutCommand(id, true), ct), "User locked out.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.Edit)]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await FlashResult(await mediator.Send(new SetUserLockoutCommand(id, false), ct), "User unlocked.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [HasPermission(Perms.Users.ResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserForEditQuery(id), ct);
        return user is null
            ? NotFound()
            : View(new ResetUserPasswordViewModel { Id = id, DisplayName = user.Email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.ResetPassword)]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await mediator.Send(new ResetUserPasswordCommand(model.Id, model.NewPassword), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Password reset.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.Users.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), ct);
        if (result.IsFailure)
        {
            TempData["flash:error"] = result.Error.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["flash:success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Perms.PermissionsAdmin.Assign)]
    public async Task<IActionResult> Permissions(Guid id, CancellationToken ct)
    {
        var overrides = await mediator.Send(new GetUserPermissionOverridesQuery(id), ct);
        if (overrides is null)
        {
            return NotFound();
        }

        var model = new UserPermissionsViewModel
        {
            UserId = overrides.UserId,
            DisplayName = overrides.DisplayName,
            Groups = await mediator.Send(new GetPermissionCatalogQuery(), ct),
            Grants = [.. overrides.Grants],
            Denies = [.. overrides.Denies],
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Perms.PermissionsAdmin.Assign)]
    public async Task<IActionResult> Permissions(Guid id, [FromForm] Dictionary<string, string> states, CancellationToken ct)
    {
        var grants = states.Where(s => s.Value == "grant").Select(s => s.Key).ToList();
        var denies = states.Where(s => s.Value == "deny").Select(s => s.Key).ToList();

        await FlashResult(await mediator.Send(new SetUserPermissionsCommand(id, grants, denies), ct), "Permission overrides saved.");
        return RedirectToAction(nameof(Permissions), new { id });
    }

    private Task FlashResult(CMS.Shared.Results.Result result, string successMessage)
    {
        if (result.IsSuccess)
        {
            TempData["flash:success"] = successMessage;
        }
        else
        {
            TempData["flash:error"] = result.Error.Message;
        }

        return Task.CompletedTask;
    }
}
