namespace CMS.Domain.Common;

/// <summary>
/// Well-known, deterministic identifiers and names used by seeding and by the
/// single-tenant default configuration. Fixed GUIDs keep seed data idempotent
/// across migrations and environments.
/// </summary>
public static class DomainDefaults
{
    /// <summary>The tenant every entity belongs to when multi-tenancy is off.</summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string DefaultTenantName = "Default";

    /// <summary>Built-in role that implicitly holds every permission.</summary>
    public const string AdministratorRole = "Administrator";

    /// <summary>Built-in role for day-to-day content management.</summary>
    public const string EditorRole = "Editor";

    /// <summary>Built-in read-only role.</summary>
    public const string ViewerRole = "Viewer";

    public static readonly Guid AdministratorRoleId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
    public static readonly Guid EditorRoleId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000002");
    public static readonly Guid ViewerRoleId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000003");

    public static readonly Guid AdministratorUserId = Guid.Parse("b2b2b2b2-0000-0000-0000-000000000001");

    /// <summary>Default administrator credentials seeded for first login. Change on first run.</summary>
    public const string AdministratorEmail = "admin@cms.local";
    public const string AdministratorUserName = "admin@cms.local";
    public const string AdministratorDefaultPassword = "Admin#12345";

    /// <summary>Default language seeded for localization.</summary>
    public const string DefaultLanguageCode = "en-US";
}
