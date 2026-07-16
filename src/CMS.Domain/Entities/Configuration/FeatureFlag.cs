using CMS.Domain.Common;

namespace CMS.Domain.Entities.Configuration;

/// <summary>
/// Toggles a feature on or off at runtime, optionally scoped to an environment or
/// tenant. Modules check flags to enable/disable functionality without a redeploy.
/// </summary>
public class FeatureFlag : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Unique flag name, e.g. "Blog.Comments".</summary>
    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? Description { get; set; }

    /// <summary>Optional environment restriction (e.g. "Development"); null = all.</summary>
    public string? Environment { get; set; }
}
