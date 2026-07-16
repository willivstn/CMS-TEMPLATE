using CMS.Application.Common.Abstractions;
using CMS.Domain.Common;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Multitenancy;

/// <summary>
/// Baseline tenant context used when multi-tenancy is disabled: always the single
/// default tenant. When tenancy is enabled, the Web layer registers a
/// request-scoped resolver (host/header based) in its place.
/// </summary>
public sealed class DefaultTenantContext(IOptions<MultiTenancyOptions> options) : ITenantContext
{
    public Guid CurrentTenantId { get; } = DomainDefaults.DefaultTenantId;

    public bool IsMultiTenantEnabled { get; } = options.Value.Enabled;
}
