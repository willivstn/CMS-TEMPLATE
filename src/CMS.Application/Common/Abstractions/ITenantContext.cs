namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Ambient tenant information for the current request. Backed by a tenant resolver
/// in the Web layer. When multi-tenancy is disabled the context always reports the
/// default tenant, so tenant-scoped queries and stamping behave identically.
/// </summary>
public interface ITenantContext
{
    /// <summary>The tenant that owns data for the current request.</summary>
    Guid CurrentTenantId { get; }

    /// <summary>Whether multi-tenancy is enabled for this deployment.</summary>
    bool IsMultiTenantEnabled { get; }
}
