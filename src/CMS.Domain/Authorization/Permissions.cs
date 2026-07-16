namespace CMS.Domain.Authorization;

/// <summary>
/// The canonical catalogue of permission names. Authorization is permission-based:
/// roles are bundles of these, and <c>[HasPermission(Permissions.Content.Edit)]</c>
/// guards controllers. Names use a stable "Group.Action" convention so they read
/// well in claims, policy names, and the admin UI.
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManageRoles = "Users.ManageRoles";
        public const string ResetPassword = "Users.ResetPassword";
        public const string Impersonate = "Users.Impersonate";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Edit = "Roles.Edit";
        public const string Delete = "Roles.Delete";
        public const string Manage = "Roles.Manage";
    }

    public static class PermissionsAdmin
    {
        public const string View = "Permissions.View";
        public const string Assign = "Permissions.Assign";
    }

    public static class Content
    {
        public const string View = "Content.View";
        public const string Create = "Content.Create";
        public const string Edit = "Content.Edit";
        public const string Delete = "Content.Delete";
        public const string Publish = "Content.Publish";
    }

    public static class Categories
    {
        public const string View = "Categories.View";
        public const string Manage = "Categories.Manage";
    }

    public static class Media
    {
        public const string View = "Media.View";
        public const string Upload = "Media.Upload";
        public const string Delete = "Media.Delete";
        public const string Manage = "Media.Manage";
    }

    public static class Menus
    {
        public const string View = "Menu.View";
        public const string Manage = "Menu.Manage";
    }

    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Manage = "Settings.Manage";
    }

    public static class Audit
    {
        public const string View = "Audit.View";
    }

    public static class Localization
    {
        public const string View = "Localization.View";
        public const string Manage = "Localization.Manage";
    }

    public static class Notifications
    {
        public const string View = "Notifications.View";
        public const string Send = "Notifications.Send";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class System
    {
        public const string ViewLogs = "System.ViewLogs";
        public const string ManageJobs = "System.ManageJobs";
        public const string ManageFeatureFlags = "System.ManageFeatureFlags";
        public const string ManageApiKeys = "System.ManageApiKeys";
        public const string ManageEmailTemplates = "System.ManageEmailTemplates";
    }
}
