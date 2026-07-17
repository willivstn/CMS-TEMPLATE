using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

// These controllers make every seeded menu link resolve to a themed
// "coming soon" page. Each is replaced by a full implementation in later phases
// (users/roles/permissions in Phase 4; content/media in Phase 5; system in 6–7).
// Keeping them here means the navigation, permissions and breadcrumbs are all
// exercisable now.

/// <summary>Base for not-yet-implemented admin sections.</summary>
public abstract class PlaceholderController : Controller
{
    protected IActionResult ComingSoon(string title)
    {
        ViewData["Title"] = title;
        return View("ComingSoon");
    }
}

// Content, Categories, Tags and Media are implemented in their own controllers (Phase 5).
// Users, Roles and Permissions are implemented in their own controllers (Phase 4).

[Area("Admin")]
[HasPermission(Permissions.Settings.View)]
public sealed class SettingsController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Settings");
}

[Area("Admin")]
[HasPermission(Permissions.Audit.View)]
public sealed class AuditController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Audit Trail");
}

[Area("Admin")]
[HasPermission(Permissions.System.ViewLogs)]
public sealed class LogsController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("System Logs");
}

[Area("Admin")]
[HasPermission(Permissions.System.ManageJobs)]
public sealed class JobsController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Background Jobs");
}

[Area("Admin")]
[HasPermission(Permissions.System.ManageFeatureFlags)]
public sealed class FeatureFlagsController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Feature Flags");
}

[Area("Admin")]
[HasPermission(Permissions.System.ManageEmailTemplates)]
public sealed class EmailTemplatesController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Email Templates");
}

[Area("Admin")]
[HasPermission(Permissions.Localization.View)]
public sealed class LocalizationController : PlaceholderController
{
    public IActionResult Index() => ComingSoon("Localization");
}
