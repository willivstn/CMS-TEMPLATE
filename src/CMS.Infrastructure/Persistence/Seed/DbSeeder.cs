using CMS.Domain.Authorization;
using CMS.Domain.Common;
using CMS.Domain.Entities.Access;
using CMS.Domain.Entities.Configuration;
using CMS.Domain.Entities.Content;
using CMS.Domain.Entities.Identity;
using CMS.Domain.Entities.Localization;
using CMS.Domain.Entities.Navigation;
using CMS.Domain.Entities.Tenancy;
using CMS.Domain.Entities.Theming;
using CMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent database seeding: safe to run on every startup. Seeds the default
/// tenant, the full permission catalogue, built-in roles and their grants, the
/// administrator account, languages, themes, core settings, the admin menu tree,
/// and a little demo content. Existing rows are detected by natural keys so
/// re-running never duplicates data.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedTenantAsync(db, cancellationToken);
        var permissions = await SeedPermissionsAsync(db, cancellationToken);
        await SeedRolesAsync(db, cancellationToken);
        await SeedRolePermissionsAsync(db, permissions, cancellationToken);
        await SeedAdminUserAsync(db, cancellationToken);
        await SeedLanguagesAsync(db, cancellationToken);
        await SeedThemesAsync(db, cancellationToken);
        await SeedSettingsAsync(db, cancellationToken);
        await SeedMenusAsync(db, permissions, cancellationToken);
        await SeedDemoContentAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTenantAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == DomainDefaults.DefaultTenantId, ct))
        {
            return;
        }

        db.Tenants.Add(new Tenant
        {
            Id = DomainDefaults.DefaultTenantId,
            Name = DomainDefaults.DefaultTenantName,
            Identifier = "default",
            IsActive = true,
            ThemeKey = "light",
        });
    }

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Name, ct);

        foreach (var def in PermissionRegistry.All)
        {
            if (existing.ContainsKey(def.Name))
            {
                continue;
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = def.Name,
                Group = def.Group,
                DisplayName = def.DisplayName,
                IsSystem = true,
            };

            db.Permissions.Add(permission);
            existing[def.Name] = permission;
        }

        return existing;
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        (Guid Id, string Name, string Description)[] roles =
        [
            (DomainDefaults.AdministratorRoleId, DomainDefaults.AdministratorRole, "Full access to every feature."),
            (DomainDefaults.EditorRoleId, DomainDefaults.EditorRole, "Creates and manages content and media."),
            (DomainDefaults.ViewerRoleId, DomainDefaults.ViewerRole, "Read-only access to the admin area."),
        ];

        var existingIds = await db.Roles.Select(r => r.Id).ToListAsync(ct);

        foreach (var (id, name, description) in roles)
        {
            if (existingIds.Contains(id))
            {
                continue;
            }

            db.Roles.Add(new ApplicationRole(name)
            {
                Id = id,
                NormalizedName = name.ToUpperInvariant(),
                Description = description,
                IsSystem = true,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            });
        }
    }

    private static async Task SeedRolePermissionsAsync(
        AppDbContext db, Dictionary<string, Permission> permissions, CancellationToken ct)
    {
        var existing = await db.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(ct);
        var existingSet = existing.Select(x => (x.RoleId, x.PermissionId)).ToHashSet();

        // Administrator: every permission.
        var adminPermissionNames = permissions.Keys;

        // Editor: content, categories, media, plus dashboard.
        string[] editorPermissionNames =
        [
            Permissions.Dashboard.View,
            Permissions.Content.View, Permissions.Content.Create, Permissions.Content.Edit,
            Permissions.Content.Delete, Permissions.Content.Publish,
            Permissions.Categories.View, Permissions.Categories.Manage,
            Permissions.Media.View, Permissions.Media.Upload, Permissions.Media.Delete, Permissions.Media.Manage,
        ];

        // Viewer: read-only surfaces.
        string[] viewerPermissionNames =
        [
            Permissions.Dashboard.View, Permissions.Content.View,
            Permissions.Categories.View, Permissions.Media.View,
        ];

        Grant(DomainDefaults.AdministratorRoleId, adminPermissionNames);
        Grant(DomainDefaults.EditorRoleId, editorPermissionNames);
        Grant(DomainDefaults.ViewerRoleId, viewerPermissionNames);

        void Grant(Guid roleId, IEnumerable<string> permissionNames)
        {
            foreach (var name in permissionNames)
            {
                if (!permissions.TryGetValue(name, out var permission))
                {
                    continue;
                }

                if (existingSet.Contains((roleId, permission.Id)))
                {
                    continue;
                }

                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permission.Id,
                });
                existingSet.Add((roleId, permission.Id));
            }
        }
    }

    private static async Task SeedAdminUserAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == DomainDefaults.AdministratorUserId, ct))
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = DomainDefaults.AdministratorUserId,
            UserName = DomainDefaults.AdministratorUserName,
            NormalizedUserName = DomainDefaults.AdministratorUserName.ToUpperInvariant(),
            Email = DomainDefaults.AdministratorEmail,
            NormalizedEmail = DomainDefaults.AdministratorEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            LanguageCode = DomainDefaults.DefaultLanguageCode,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        admin.PasswordHash = new PasswordHasher<ApplicationUser>()
            .HashPassword(admin, DomainDefaults.AdministratorDefaultPassword);

        db.Users.Add(admin);
        db.Set<ApplicationUserRole>().Add(new ApplicationUserRole
        {
            UserId = admin.Id,
            RoleId = DomainDefaults.AdministratorRoleId,
        });
    }

    private static async Task SeedLanguagesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Languages.AnyAsync(ct))
        {
            return;
        }

        db.Languages.AddRange(
            new Language { Id = Guid.NewGuid(), Code = "en-US", Name = "English", NativeName = "English", IsDefault = true, IsEnabled = true, SortOrder = 0 },
            new Language { Id = Guid.NewGuid(), Code = "ar-SA", Name = "Arabic", NativeName = "العربية", IsRtl = true, IsEnabled = true, SortOrder = 1 },
            new Language { Id = Guid.NewGuid(), Code = "es-ES", Name = "Spanish", NativeName = "Español", IsEnabled = true, SortOrder = 2 });
    }

    private static async Task SeedThemesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Themes.AnyAsync(ct))
        {
            return;
        }

        db.Themes.AddRange(
            new Theme { Id = Guid.NewGuid(), Key = "light", Name = "Light", IsDark = false, IsDefault = true, IsEnabled = true, SortOrder = 0 },
            new Theme { Id = Guid.NewGuid(), Key = "dark", Name = "Dark", IsDark = true, IsDefault = false, IsEnabled = true, SortOrder = 1 });
    }

    private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        var existingKeys = await db.Settings.Select(s => s.Key).ToListAsync(ct);

        (string Key, string Value, SettingType Type, string Group, bool IsPublic)[] settings =
        [
            ("Site.Title", "CMS Template", SettingType.String, "General", true),
            ("Site.Description", "A production-ready CMS starter.", SettingType.String, "General", true),
            ("Site.DefaultLanguage", DomainDefaults.DefaultLanguageCode, SettingType.String, "General", true),
            ("Site.DefaultTheme", "light", SettingType.String, "Appearance", true),
            ("Security.AllowRegistration", "true", SettingType.Boolean, "Security", false),
            ("Security.RequireEmailConfirmation", "true", SettingType.Boolean, "Security", false),
            ("Content.ItemsPerPage", "10", SettingType.Integer, "Content", false),
        ];

        foreach (var (key, value, type, group, isPublic) in settings)
        {
            if (existingKeys.Contains(key))
            {
                continue;
            }

            db.Settings.Add(new Setting
            {
                Id = Guid.NewGuid(),
                TenantId = DomainDefaults.DefaultTenantId,
                Key = key,
                Value = value,
                Type = type,
                Group = group,
                IsPublic = isPublic,
                IsSystem = true,
            });
        }
    }

    private static async Task SeedMenusAsync(
        AppDbContext db, Dictionary<string, Permission> permissions, CancellationToken ct)
    {
        if (await db.MenuItems.AnyAsync(ct))
        {
            return;
        }

        // Top-level groups.
        var dashboard = Menu("Dashboard", "bi-speedometer2", "Dashboard", "Index", 0, Permissions.Dashboard.View);
        var content = Menu("Content", "bi-file-earmark-text", null, null, 1, Permissions.Content.View);
        var media = Menu("Media Library", "bi-images", "Media", "Index", 2, Permissions.Media.View);
        var access = Menu("Access Control", "bi-shield-lock", null, null, 3, Permissions.Users.View);
        var system = Menu("System", "bi-gear", null, null, 4, Permissions.Settings.View);

        db.MenuItems.AddRange(dashboard, content, media, access, system);

        // Content children.
        Child(content, "Pages", "bi-file-earmark", "Content", "Pages", 0, Permissions.Content.View);
        Child(content, "Posts", "bi-newspaper", "Content", "Posts", 1, Permissions.Content.View);
        Child(content, "Categories", "bi-diagram-3", "Categories", "Index", 2, Permissions.Categories.View);
        Child(content, "Tags", "bi-tags", "Tags", "Index", 3, Permissions.Content.View);

        // Access children.
        Child(access, "Users", "bi-people", "Users", "Index", 0, Permissions.Users.View);
        Child(access, "Roles", "bi-person-badge", "Roles", "Index", 1, Permissions.Roles.View);
        Child(access, "Permissions", "bi-key", "Permissions", "Index", 2, Permissions.PermissionsAdmin.View);

        // System children.
        Child(system, "Settings", "bi-sliders", "Settings", "Index", 0, Permissions.Settings.View);
        Child(system, "Audit Trail", "bi-clipboard-check", "Audit", "Index", 1, Permissions.Audit.View);
        Child(system, "System Logs", "bi-journal-text", "Logs", "Index", 2, Permissions.System.ViewLogs);
        Child(system, "Background Jobs", "bi-cpu", "Jobs", "Index", 3, Permissions.System.ManageJobs);
        Child(system, "Feature Flags", "bi-toggles", "FeatureFlags", "Index", 4, Permissions.System.ManageFeatureFlags);
        Child(system, "Email Templates", "bi-envelope", "EmailTemplates", "Index", 5, Permissions.System.ManageEmailTemplates);
        Child(system, "Localization", "bi-translate", "Localization", "Index", 6, Permissions.Localization.View);

        MenuItem Menu(string title, string icon, string? controller, string? action, int order, string permission)
        {
            var item = new MenuItem
            {
                Id = Guid.NewGuid(),
                TenantId = DomainDefaults.DefaultTenantId,
                Title = title,
                Icon = icon,
                Controller = controller,
                Action = action,
                Area = "Admin",
                SortOrder = order,
                IsVisible = true,
                IsEnabled = true,
            };
            AttachPermission(item, permission);
            return item;
        }

        void Child(MenuItem parent, string title, string icon, string controller, string action, int order, string permission)
        {
            var item = new MenuItem
            {
                Id = Guid.NewGuid(),
                TenantId = DomainDefaults.DefaultTenantId,
                Parent = parent,
                Title = title,
                Icon = icon,
                Controller = controller,
                Action = action,
                Area = "Admin",
                SortOrder = order,
                IsVisible = true,
                IsEnabled = true,
            };
            AttachPermission(item, permission);
            db.MenuItems.Add(item);
        }

        void AttachPermission(MenuItem item, string permissionName)
        {
            if (permissions.TryGetValue(permissionName, out var permission))
            {
                item.MenuPermissions.Add(new MenuItemPermission
                {
                    Id = Guid.NewGuid(),
                    PermissionId = permission.Id,
                });
            }
        }
    }

    private static async Task SeedDemoContentAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.ContentItems.AnyAsync(ct))
        {
            return;
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = DomainDefaults.DefaultTenantId,
            Name = "General",
            Slug = "general",
            Description = "Default category.",
        };
        db.Categories.Add(category);

        db.ContentItems.Add(new ContentItem
        {
            Id = Guid.NewGuid(),
            TenantId = DomainDefaults.DefaultTenantId,
            Type = ContentType.Page,
            Status = ContentStatus.Published,
            Title = "Welcome",
            Slug = "welcome",
            Summary = "Welcome to the CMS template.",
            Body = "<h1>Welcome</h1><p>This page was created by the database seeder.</p>",
            CategoryId = category.Id,
            AuthorId = DomainDefaults.AdministratorUserId,
            IsHomePage = true,
            PublishedOnUtc = DateTimeOffset.UtcNow,
        });
    }
}
