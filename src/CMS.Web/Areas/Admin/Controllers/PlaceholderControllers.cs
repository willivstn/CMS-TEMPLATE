using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

// Remaining scaffolded sections that keep the navigation fully navigable until
// their own phase. Content/Categories/Tags/Media = Phase 5; Users/Roles/Permissions
// = Phase 4; Settings/Logs/Jobs/FeatureFlags/EmailTemplates/Notifications = Phase 6.

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
[HasPermission(Permissions.Audit.View)]
public sealed class AuditController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Audit Trail");
}

[Area("Admin")]
[HasPermission(Permissions.Localization.View)]
public sealed class LocalizationController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Localization");
}
