using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

// Remaining scaffolded section. Localization (resource/DB translation management)
// is left as a documented extension point.

/// <summary>Base for not-yet-implemented admin sections.</summary>
public abstract class PlaceholderController : Controller
{
    protected IActionResult ComingSoon(string title)
    {
        ViewData["Title"] = title;
        return View("ComingSoon");
    }
}

[Area("Admin")]
[HasPermission(Permissions.Localization.View)]
public sealed class LocalizationController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Localization");
}
