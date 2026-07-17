using CMS.Application.Features.EmailTemplates;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Manages reusable, tokenized email templates.</summary>
[Area("Admin")]
[HasPermission(Permissions.System.ManageEmailTemplates)]
public sealed class EmailTemplatesController(ISender mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetEmailTemplatesQuery(), ct));

    [HttpGet]
    public IActionResult Create() => View(new EmailTemplateFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmailTemplateFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await mediator.Send(new CreateEmailTemplateCommand(model.Key, model.Subject, model.HtmlBody, model.TextBody, model.Description, model.IsActive), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Template created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var t = await mediator.Send(new GetEmailTemplateForEditQuery(id), ct);
        return t is null
            ? NotFound()
            : View(new EmailTemplateFormViewModel
            {
                Id = t.Id, Key = t.Key, Subject = t.Subject, HtmlBody = t.HtmlBody,
                TextBody = t.TextBody, Description = t.Description, IsActive = t.IsActive, AvailableTokens = t.AvailableTokens,
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmailTemplateFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            return View(model);
        }

        var result = await mediator.Send(new UpdateEmailTemplateCommand(model.Id.Value, model.Subject, model.HtmlBody, model.TextBody, model.Description, model.IsActive), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["flash:success"] = "Template saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteEmailTemplateCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Template deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
