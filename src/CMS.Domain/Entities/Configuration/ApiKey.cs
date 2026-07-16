using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;

namespace CMS.Domain.Entities.Configuration;

/// <summary>
/// A hashed API key for programmatic access to the REST API. Only a short,
/// non-secret <see cref="Prefix"/> is shown after creation; the full key is
/// stored as a one-way <see cref="KeyHash"/>. Scoped and expirable.
/// </summary>
public class ApiKey : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short public prefix used to look up the key before hash comparison.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the full key value.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Optional owning user.</summary>
    public Guid? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    /// <summary>Space- or comma-separated scopes limiting what the key can do.</summary>
    public string? Scopes { get; set; }

    public DateTimeOffset? ExpiresOnUtc { get; set; }
    public DateTimeOffset? LastUsedUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsExpired => ExpiresOnUtc is { } exp && DateTimeOffset.UtcNow >= exp;
}
