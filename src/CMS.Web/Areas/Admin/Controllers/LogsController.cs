using CMS.Application.Features.Observability;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>In-app viewer for warning-and-above system logs.</summary>
[Area("Admin")]
[HasPermission(Permissions.System.ViewLogs)]
public sealed class LogsController(ISender mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? level, string? search, int page = 1, CancellationToken ct = default)
    {
        ViewBag.Level = level;
        ViewBag.Search = search;
        return View(await mediator.Send(new GetSystemLogsQuery(level, search, page), ct));
    }
}
