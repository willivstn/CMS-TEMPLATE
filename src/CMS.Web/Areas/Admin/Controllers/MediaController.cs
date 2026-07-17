using CMS.Application.Features.Media;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using CMS.Web.Storage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>The media library: folder navigation, uploads (with thumbnails) and a picker.</summary>
[Area("Admin")]
public sealed class MediaController(ISender mediator, MediaUploadService uploads) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Media.View)]
    public async Task<IActionResult> Index(Guid? folderId, string? search, int page = 1, CancellationToken ct = default)
    {
        var model = new MediaLibraryViewModel
        {
            CurrentFolderId = folderId,
            Search = search,
            Folders = await mediator.Send(new GetMediaFoldersQuery(folderId), ct),
            Items = await mediator.Send(new GetMediaItemsQuery(folderId, search, page), ct),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Media.Upload)]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(Guid? folderId, List<IFormFile> files, CancellationToken ct)
    {
        var uploaded = 0;
        var errors = new List<string>();

        foreach (var file in files)
        {
            var result = await uploads.UploadAsync(file, folderId, ct);
            if (result.IsSuccess)
            {
                uploaded++;
            }
            else
            {
                errors.Add(result.Error.Message);
            }
        }

        TempData["flash:success"] = $"{uploaded} file(s) uploaded.";
        if (errors.Count > 0)
        {
            TempData["flash:error"] = string.Join(" ", errors);
        }

        return RedirectToAction(nameof(Index), new { folderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Media.Manage)]
    public async Task<IActionResult> CreateFolder(string name, Guid? parentId, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateMediaFolderCommand(name ?? string.Empty, parentId), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Folder created." : result.Error.Message;
        return RedirectToAction(nameof(Index), new { folderId = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Media.Manage)]
    public async Task<IActionResult> DeleteFolder(Guid id, Guid? parentId, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteMediaFolderCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Folder deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index), new { folderId = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Media.Manage)]
    public async Task<IActionResult> EditMeta(MediaMetaViewModel model, Guid? folderId, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateMediaItemCommand(model.Id, model.AltText, model.Title), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Metadata saved." : result.Error.Message;
        return RedirectToAction(nameof(Index), new { folderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Media.Delete)]
    public async Task<IActionResult> Delete(Guid id, Guid? folderId, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteMediaItemCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "File deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index), new { folderId });
    }

    /// <summary>Returns a grid of selectable images for the media picker modal.</summary>
    [HttpGet]
    [HasPermission(Permissions.Media.View)]
    public async Task<IActionResult> Picker(string? search, CancellationToken ct)
    {
        var items = await mediator.Send(new GetMediaItemsQuery(null, search, 1, 60), ct);
        return PartialView("_MediaPicker", items);
    }
}
