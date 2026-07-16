using CMS.Domain.Common;

namespace CMS.Domain.Entities.Tenancy;

/// <summary>
/// An isolation boundary for tenant-scoped data. The template ships with a single
/// default tenant (<see cref="DomainDefaults.DefaultTenantId"/>); enabling
/// multi-tenancy activates per-tenant resolution and query filtering without any
/// entity changes.
/// </summary>
public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Stable identifier used in resolution (e.g. host or header value).</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Optional host name used for host-based tenant resolution.</summary>
    public string? Host { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Key of the theme applied to this tenant, if any.</summary>
    public string? ThemeKey { get; set; }
}
