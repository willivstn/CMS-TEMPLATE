using CMS.Application.Features.Permissions;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Read-only catalogue of all permissions, grouped by area.</summary>
[Area("Admin")]
public sealed class PermissionsController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.PermissionsAdmin.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetPermissionCatalogQuery(), ct));
}
