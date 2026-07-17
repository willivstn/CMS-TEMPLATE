using CMS.Application.Features.Localization;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Manages UI languages and database-backed translations.</summary>
[Area("Admin")]
public sealed class LocalizationController(ISender mediator) : Controller
{
    // ---- Languages ----

    [HttpGet]
    [HasPermission(Permissions.Localization.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetLanguagesQuery(), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> CreateLanguage(LanguageFormViewModel model, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateLanguageCommand(model.Code, model.Name, model.NativeName, model.IsRtl, model.IsEnabled, model.SortOrder), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Language added." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> EditLanguage(Guid id, CancellationToken ct)
    {
        var language = (await mediator.Send(new GetLanguagesQuery(), ct)).FirstOrDefault(l => l.Id == id);
        return language is null
            ? NotFound()
            : View(new LanguageFormViewModel { Id = language.Id, Code = language.Code, Name = language.Name, NativeName = language.NativeName, IsRtl = language.IsRtl, IsEnabled = language.IsEnabled, SortOrder = language.SortOrder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> EditLanguage(LanguageFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var result = await mediator.Send(new UpdateLanguageCommand(model.Id.Value, model.Name, model.NativeName, model.IsRtl, model.IsEnabled, model.SortOrder), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Language saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> SetDefaultLanguage(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SetDefaultLanguageCommand(id), ct);
        TempData["flash:success"] = "Default language updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteLanguageCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Language deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    // ---- Translations ----

    [HttpGet]
    [HasPermission(Permissions.Localization.View)]
    public async Task<IActionResult> Translations(string? language, string? group, string? search, int page = 1, CancellationToken ct = default)
    {
        ViewBag.Language = language;
        ViewBag.Group = group;
        ViewBag.Search = search;
        ViewBag.Languages = await mediator.Send(new GetLanguageOptionsQuery(), ct);
        return View(await mediator.Send(new GetTranslationsQuery(language, group, search, page), ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> CreateTranslation(TranslationFormViewModel model, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTranslationCommand(model.Key, model.LanguageCode, model.Value, model.Group), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Translation added." : result.Error.Message;
        return RedirectToAction(nameof(Translations), new { language = model.LanguageCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> UpdateTranslation(Guid id, string value, string? group, CancellationToken ct)
    {
        await mediator.Send(new UpdateTranslationCommand(id, value ?? string.Empty, group), ct);
        TempData["flash:success"] = "Translation saved.";
        return RedirectToAction(nameof(Translations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Localization.Manage)]
    public async Task<IActionResult> DeleteTranslation(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteTranslationCommand(id), ct);
        TempData["flash:success"] = "Translation deleted.";
        return RedirectToAction(nameof(Translations));
    }
}
