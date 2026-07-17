using CMS.Application.Features.Categories;
using CMS.Domain.Authorization;
using CMS.Web.Areas.Admin.Models;
using CMS.Web.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

/// <summary>Category management (hierarchical taxonomy for content).</summary>
[Area("Admin")]
public sealed class CategoriesController(ISender mediator) : Controller
{
    [HttpGet]
    [HasPermission(Permissions.Categories.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await mediator.Send(new GetCategoriesQuery(), ct));

    [HttpGet]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(new CategoryFormViewModel { Parents = await mediator.Send(new GetCategoryParentOptionsQuery(), ct) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await mediator.Send(new GetCategoryParentOptionsQuery(), ct);
            return View(model);
        }

        var result = await mediator.Send(new CreateCategoryCommand(model.Name, model.Slug, model.Description, model.ParentId, model.SortOrder), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.Parents = await mediator.Send(new GetCategoryParentOptionsQuery(), ct);
            return View(model);
        }

        TempData["flash:success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var category = await mediator.Send(new GetCategoryForEditQuery(id), ct);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            Parents = await mediator.Send(new GetCategoryParentOptionsQuery(category.Id), ct),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Edit(CategoryFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || model.Id is null)
        {
            model.Parents = await mediator.Send(new GetCategoryParentOptionsQuery(model.Id), ct);
            return View(model);
        }

        var result = await mediator.Send(new UpdateCategoryCommand(model.Id.Value, model.Name, model.Slug, model.Description, model.ParentId, model.SortOrder), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.Parents = await mediator.Send(new GetCategoryParentOptionsQuery(model.Id), ct);
            return View(model);
        }

        TempData["flash:success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteCategoryCommand(id), ct);
        TempData[result.IsSuccess ? "flash:success" : "flash:error"] = result.IsSuccess ? "Category deleted." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }
}
