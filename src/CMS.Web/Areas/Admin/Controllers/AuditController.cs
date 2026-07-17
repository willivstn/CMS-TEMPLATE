using CMS.Application.Features.Observability;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Read-only audit-trail viewer (paged, filterable).</summary>
[Area("Admin")]
[HasPermission(Permissions.Audit.View)]
public sealed class AuditController(ISender mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? entity, string? search, int page = 1, CancellationToken ct = default)
    {
        ViewBag.Entity = entity;
        ViewBag.Search = search;
        return View(await mediator.Send(new GetAuditLogsQuery(entity, search, page), ct));
    }
}
