using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Configuration;

/// <summary>
/// A database-driven application setting. Typed via <see cref="Type"/>, grouped
/// for the admin UI, and optionally encrypted at rest (via Data Protection) for
/// sensitive values. <see cref="IsPublic"/> settings may be exposed to the client.
/// </summary>
public class Setting : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Unique key within a tenant, e.g. "Site.Title".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The raw stored value (ciphertext when <see cref="IsEncrypted"/>).</summary>
    public string? Value { get; set; }

    public SettingType Type { get; set; } = SettingType.String;

    /// <summary>UI grouping, e.g. "General", "Email", "Security".</summary>
    public string Group { get; set; } = "General";

    public string? Description { get; set; }

    /// <summary>When true, <see cref="Value"/> is stored encrypted.</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>System settings are created by seeding and cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    /// <summary>When true the setting may be surfaced to unauthenticated clients.</summary>
    public bool IsPublic { get; set; }
}
