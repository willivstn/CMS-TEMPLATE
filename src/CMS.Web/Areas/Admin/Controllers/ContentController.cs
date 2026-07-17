using CMS.Application.Features.Content;
using CMS.Domain.Authorization;
using CMS.Domain.Enums;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>
/// Pages and posts (both <see cref="ContentType"/> values of the content
/// aggregate): CRUD, editorial workflow, and revision history.
/// </summary>
[Area("Admin")]
public sealed class ContentController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Content.View)]
    public Task<IActionResult> Pages(string? search, ContentStatus? status, int page = 1, CancellationToken ct = default)
        => ListAsync(ContentType.Page, "Pages", search, status, page, ct);

    [HttpGet]
    [HasPermission(Permissions.Content.View)]
    public Task<IActionResult> Posts(string? search, ContentStatus? status, int page = 1, CancellationToken ct = default)
        => ListAsync(ContentType.Post, "Posts", search, status, page, ct);

    private async Task<IActionResult> ListAsync(ContentType type, string title, string? search, ContentStatus? status, int page, CancellationToken ct)
    {
        var result = await mediator.Send(new GetContentListQuery(type, search, status, page), ct);
        ViewBag.Type = type;
        ViewBag.Title = title;
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View("Index", result);
    }

    [HttpGet]
    [HasPermission(Permissions.Content.Create)]
    public async Task<IActionResult> Create(ContentType type = ContentType.Page, CancellationToken ct = default)
    {
        var model = new ContentFormViewModel { Type = type, Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct) };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Create)]
    public async Task<IActionResult> Create(ContentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct);
            return View(model);
        }

        var result = await mediator.Send(new CreateContentCommand(
            model.Type, model.Title, model.Slug, model.Summary, model.Body,
            model.MetaTitle, model.MetaDescription, model.MetaKeywords, model.CanonicalUrl, model.OpenGraphImageUrl,
            model.CategoryId, model.FeaturedMediaId, model.IsHomePage, model.Status, null, model.TagList()), ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct);
            return View(model);
        }

        TempData["flash:success"] = "Content created.";
        return RedirectToAction(nameof(Edit), new { id = result.Value });
    }

    [HttpGet]
    [HasPermission(Permissions.Content.Edit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var content = await mediator.Send(new GetContentForEditQuery(id), ct);
        if (content is null)
        {
            return NotFound();
        }

        var model = new ContentFormViewModel
        {
            Id = content.Id,
            Type = content.Type,
            Status = content.Status,
            Title = content.Title,
            Slug = content.Slug,
            Summary = content.Summary,
            Body = content.Body,
            MetaTitle = content.MetaTitle,
            MetaDescription = content.MetaDescription,
            MetaKeywords = content.MetaKeywords,
            CanonicalUrl = content.CanonicalUrl,
            OpenGraphImageUrl = content.OpenGraphImageUrl,
            CategoryId = content.CategoryId,
            FeaturedMediaId = content.FeaturedMediaId,
            FeaturedMediaUrl = content.FeaturedMediaUrl,
            IsHomePage = content.IsHomePage,
            Tags = string.Join(", ", content.Tags),
            Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Edit)]
    public async Task<IActionResult> Edit(ContentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            model.Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct);
            return View(model);
        }

        var result = await mediator.Send(new UpdateContentCommand(
            model.Id.Value, model.Title, model.Slug, model.Summary, model.Body,
            model.MetaTitle, model.MetaDescription, model.MetaKeywords, model.CanonicalUrl, model.OpenGraphImageUrl,
            model.CategoryId, model.FeaturedMediaId, model.IsHomePage, model.TagList()), ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.Categories = await mediator.Send(new GetCategoryOptionsQuery(), ct);
            return View(model);
        }

        TempData["flash:success"] = "Content saved.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpGet]
    [HasPermission(Permissions.Content.View)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var preview = await mediator.Send(new GetContentPreviewQuery(id), ct);
        return preview is null ? NotFound() : View(preview);
    }

    [HttpGet]
    [HasPermission(Permissions.Content.View)]
    public async Task<IActionResult> Revisions(Guid id, CancellationToken ct)
    {
        ViewBag.ContentId = id;
        return View(await mediator.Send(new GetContentVersionsQuery(id), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Edit)]
    public async Task<IActionResult> Restore(Guid id, int version, CancellationToken ct)
    {
        await Flash(await mediator.Send(new RestoreContentVersionCommand(id, version), ct), "Revision restored.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Publish)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await Flash(await mediator.Send(new PublishContentCommand(id), ct), "Published.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Publish)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await Flash(await mediator.Send(new UnpublishContentCommand(id), ct), "Moved to draft.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Publish)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        await Flash(await mediator.Send(new ArchiveContentCommand(id), ct), "Archived.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Publish)]
    public async Task<IActionResult> Schedule(Guid id, DateTimeOffset when, CancellationToken ct)
    {
        await Flash(await mediator.Send(new ScheduleContentCommand(id, when), ct), "Scheduled.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Content.Delete)]
    public async Task<IActionResult> Delete(Guid id, ContentType type, CancellationToken ct)
    {
        await Flash(await mediator.Send(new DeleteContentCommand(id), ct), "Content deleted.");
        return RedirectToAction(type == ContentType.Post ? nameof(Posts) : nameof(Pages));
    }

    private Task Flash(CMS.Shared.Results.Result result, string success)
    {
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? success : result.Error.Message;
        return Task.CompletedTask;
    }
}
