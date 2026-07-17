using CMS.Application.Features.Tags;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Tag management. Tags are also created on the fly when editing content.</summary>
[Area("Admin")]
public sealed class TagsController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Content.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetTagsQuery(), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Edit)]
    public async Task<IActionResult> Create(string name, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTagCommand(name ?? string.Empty), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Tag created." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteTagCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Tag deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
