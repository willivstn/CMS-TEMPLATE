# Database Schema & Relationships

The CMS template ships a normalized SQL Server schema of **36 tables** managed by
EF Core. This document describes the entity groups, their relationships, and the
cross-cutting conventions. The diagrams use Mermaid and render on GitHub.

## Conventions

- **Keys** — all domain tables use `uniqueidentifier` (GUID) primary keys
  (`Entity<Guid>`). Identity tables also use GUID keys.
- **Auditing** — tables whose entities implement `IAuditable` carry
  `CreatedOnUtc/CreatedBy/ModifiedOnUtc/ModifiedBy`, stamped automatically by a
  SaveChanges interceptor.
- **Soft delete** — `ISoftDeletable` tables carry `IsDeleted/DeletedOnUtc/DeletedBy`;
  a global query filter hides deleted rows and hard deletes are rewritten to soft
  deletes.
- **Multi-tenancy** — `ITenantEntity` tables carry `TenantId`; a global query
  filter scopes them to the current tenant (single default tenant out of the box).
- **Enums** — persisted as strings (e.g. `ContentStatus`, `AuditAction`) for
  readability and stability.
- **Audit trail** — every insert/update/delete on an auditable entity is captured
  as an `AuditLogs` row with before/after JSON snapshots.

## Entity groups

| Group | Tables |
|-------|--------|
| **Identity & Access** | Users, Roles, UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims, Permissions, RolePermissions, UserPermissions, LoginHistories, RefreshTokens |
| **Navigation** | MenuItems, MenuItemPermissions |
| **Content** | ContentItems, ContentVersions, Categories, Tags, ContentTags |
| **Media** | MediaFolders, MediaItems |
| **Configuration** | Settings, FeatureFlags, ApiKeys |
| **Localization** | Languages, Translations |
| **Notifications** | Notifications, NotificationRecipients |
| **Communication** | EmailTemplates, EmailMessages |
| **System / Observability** | AuditLogs, SystemLogs, JobRuns, ScheduledTasks |
| **Tenancy / Theming** | Tenants, Themes |

## Access control (permission-based authorization)

```mermaid
erDiagram
    Users ||--o{ UserRoles : has
    Roles ||--o{ UserRoles : has
    Roles ||--o{ RolePermissions : grants
    Permissions ||--o{ RolePermissions : "granted to"
    Users ||--o{ UserPermissions : "override"
    Permissions ||--o{ UserPermissions : "override"
    Users ||--o{ LoginHistories : records
    Users ||--o{ RefreshTokens : issues
    MenuItems ||--o{ MenuItemPermissions : "gated by"
    Permissions ||--o{ MenuItemPermissions : reveals
    MenuItems ||--o{ MenuItems : "parent of"

    Users {
        guid Id PK
        string UserName
        string Email
        bool IsActive
        bool IsDeleted
    }
    Roles {
        guid Id PK
        string Name
        bool IsSystem
    }
    Permissions {
        guid Id PK
        string Name UK
        string Group
    }
    RolePermissions {
        guid Id PK
        guid RoleId FK
        guid PermissionId FK
    }
    UserPermissions {
        guid Id PK
        guid UserId FK
        guid PermissionId FK
        bool IsGranted
    }
    MenuItems {
        guid Id PK
        guid ParentId FK
        string Title
        int SortOrder
    }
```

**Effective permissions** = (union of the user's roles' permissions) **+** explicit
user grants **−** explicit user denies. Menu visibility is filtered by the same set.

## Content & media

```mermaid
erDiagram
    ContentItems ||--o{ ContentVersions : "revisions"
    ContentItems ||--o{ ContentTags : tagged
    Tags ||--o{ ContentTags : tags
    Categories ||--o{ ContentItems : categorizes
    Categories ||--o{ Categories : "parent of"
    ContentItems ||--o{ ContentItems : "parent of"
    Users ||--o{ ContentItems : authors
    MediaItems ||--o{ ContentItems : "featured in"
    MediaFolders ||--o{ MediaItems : contains
    MediaFolders ||--o{ MediaFolders : "parent of"

    ContentItems {
        guid Id PK
        guid TenantId FK
        string Type
        string Status
        string Title
        string Slug UK
        guid CategoryId FK
        guid AuthorId FK
        guid FeaturedMediaId FK
        guid ParentId FK
        datetime PublishedOnUtc
    }
    ContentVersions {
        guid Id PK
        guid ContentItemId FK
        int VersionNumber
        string Status
    }
    Categories {
        guid Id PK
        string Name
        string Slug UK
        guid ParentId FK
    }
    Tags {
        guid Id PK
        string Name
        string Slug UK
    }
    MediaItems {
        guid Id PK
        guid FolderId FK
        string Type
        string StorageKey
        bool IsDeleted
    }
```

## Platform (config, localization, notifications, system)

```mermaid
erDiagram
    Tenants ||..o{ Settings : scopes
    Tenants ||..o{ ContentItems : scopes
    Languages ||--o{ Translations : provides
    Notifications ||--o{ NotificationRecipients : "delivered to"
    Users ||--o{ NotificationRecipients : receives
    EmailTemplates ||--o{ EmailMessages : renders
    ApiKeys }o--|| Users : "owned by"

    Settings {
        guid Id PK
        guid TenantId FK
        string Key
        string Type
        bool IsEncrypted
    }
    Languages {
        guid Id PK
        string Code UK
        bool IsRtl
        bool IsDefault
    }
    AuditLogs {
        guid Id PK
        guid TenantId
        guid UserId
        string Action
        string EntityName
        string OldValues
        string NewValues
    }
    FeatureFlags {
        guid Id PK
        string Name
        bool IsEnabled
    }
    JobRuns {
        guid Id PK
        string JobName
        string Status
        int Progress
    }
```

> The `Tenants ||..o{` relationships are logical (enforced by query filters and the
> `TenantId` column) rather than physical foreign keys, so tenancy can be toggled
> without schema changes.

## Regenerating

```bash
# Add a migration after changing the model
dotnet ef migrations add <Name> \
  --project src/CMS.Infrastructure --startup-project src/CMS.Web \
  --output-dir Persistence/Migrations

# Apply to the database
dotnet ef database update --project src/CMS.Infrastructure --startup-project src/CMS.Web
```

The database is also migrated and seeded automatically on application startup
(`DatabaseInitializer`).
