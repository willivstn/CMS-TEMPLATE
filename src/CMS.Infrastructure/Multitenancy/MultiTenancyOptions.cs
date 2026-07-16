namespace CMS.Infrastructure.Multitenancy;

/// <summary>
/// Binds the "MultiTenancy" configuration section. Disabled by default: the app
/// runs as a single tenant using <see cref="CMS.Domain.Common.DomainDefaults.DefaultTenantId"/>.
/// </summary>
public sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>When true, per-request tenant resolution and query filtering apply.</summary>
    public bool Enabled { get; set; }

    /// <summary>How tenants are resolved when enabled: "Host" or "Header".</summary>
    public string ResolutionStrategy { get; set; } = "Host";

    /// <summary>Header name used when <see cref="ResolutionStrategy"/> is "Header".</summary>
    public string HeaderName { get; set; } = "X-Tenant";
}
