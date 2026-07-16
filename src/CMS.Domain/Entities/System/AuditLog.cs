using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.System;

/// <summary>
/// An append-only audit-trail entry capturing who changed what, when, and from
/// where. Populated automatically by the auditing interceptor for entity changes
/// and explicitly for security events (login, permission/role/setting changes).
/// Stores before/after snapshots as JSON.
/// </summary>
public class AuditLog : Entity<Guid>, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid? UserId { get; set; }

    /// <summary>Denormalised user name so the trail survives user deletion.</summary>
    public string? UserName { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>The affected entity type name, e.g. "ContentItem".</summary>
    public string? EntityName { get; set; }

    /// <summary>The affected entity's primary key (as text).</summary>
    public string? EntityId { get; set; }

    /// <summary>JSON snapshot of changed values before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of changed values after the change.</summary>
    public string? NewValues { get; set; }

    /// <summary>Comma-separated list of columns that changed.</summary>
    public string? AffectedColumns { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
