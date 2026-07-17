# Extending the template — adding a module

A "module" here is a **vertical slice**: a feature that plugs into the existing
infrastructure (persistence, permissions, navigation, CQRS, admin UI, audit) with
**no changes to the core**. The shipped **Announcements** module is a worked
example — follow the same seven steps to add your own (Blog, CRM, Bookings, …).

Use the Announcements files as a reference:

| Step | What | Announcements example |
|------|------|-----------------------|
| 1 | **Domain entity** | `src/CMS.Domain/Entities/Modules/Announcement.cs` |
| 2 | **EF configuration + DbSet** | `Persistence/Configurations/ModuleConfigurations.cs`, `IApplicationDbContext`, `AppDbContext` |
| 3 | **Migration** | `dotnet ef migrations add AddAnnouncementsModule` |
| 4 | **Permissions** | `Permissions.Announcements` in `src/CMS.Domain/Authorization/Permissions.cs` |
| 5 | **CQRS feature** | `src/CMS.Application/Features/Announcements/AnnouncementsFeature.cs` |
| 6 | **Admin controller + views** | `Areas/Admin/Controllers/AnnouncementsController.cs`, `Areas/Admin/Views/Announcements/*` |
| 7 | **Navigation (seed)** | menu entry added in `DbSeeder` |

## 1. Entity

Derive from `AuditableEntity` (Guid key + audit + soft delete) and implement
`ITenantEntity` if the data is tenant-scoped. That single choice gives you audit
stamping, soft delete + restore, and tenant filtering for free via the SaveChanges
interceptors and global query filters.

```csharp
public class Announcement : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOnUtc { get; set; }
}
```

## 2. Configuration + DbSet

Add an `IEntityTypeConfiguration<T>` (auto-discovered by
`ApplyConfigurationsFromAssembly`), then expose the set on `IApplicationDbContext`
and `AppDbContext`.

## 3. Migration

```bash
dotnet ef migrations add AddAnnouncementsModule \
  --project src/CMS.Infrastructure --startup-project src/CMS.Web \
  --output-dir Persistence/Migrations
```

It is applied automatically on startup by `DatabaseInitializer`.

## 4. Permissions

Add a nested class to `Permissions`. `PermissionRegistry` discovers it by
reflection, the seeder inserts the rows, and the admin permission matrix picks it
up — nothing else to wire.

```csharp
public static class Announcements
{
    public const string View = "Announcements.View";
    public const string Manage = "Announcements.Manage";
}
```

## 5. CQRS feature

Write records implementing `IRequest<Result<T>>` / `IRequest<Result>` plus their
handlers and FluentValidation validators. They are registered automatically
(`AddApplication` scans the assembly) and the validation pipeline behaviour runs
before every handler.

## 6. Admin controller + views

A thin `[Area("Admin")]` controller dispatches via `ISender` and guards actions
with `[HasPermission(Permissions.Announcements.Manage)]`. Views reuse the shared
components (`_Pager`, `_Flash`, `data-cms-confirm`, the admin layout).

## 7. Navigation

Add a menu row in `DbSeeder`. Menu items are permission-filtered automatically, so
your entry only appears for users who hold its permission. For already-seeded
databases, add it to the idempotent `EnsureExtraMenusAsync` so existing installs
pick it up on the next startup.

---

That's it — the module now participates in **auth, permissions, navigation,
audit, multi-tenancy, and the REST API surface** without touching the core.
Everything the core features use is available to modules through the same
abstractions (`IApplicationDbContext`, `ICurrentUser`, `INotificationService`,
`IBackgroundJobService`, `IFileStorage`, `ISettingService`, `IFeatureService`).
