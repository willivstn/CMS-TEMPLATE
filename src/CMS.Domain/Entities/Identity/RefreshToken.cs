using CMS.Domain.Common;

namespace CMS.Domain.Entities.Identity;

/// <summary>
/// A rotating refresh token backing JWT authentication for the REST API. Stored
/// hashed; rotated on use and revocable. Consumed by the API layer in Phase 7.
/// </summary>
public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 hash of the token value; the raw token is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresOnUtc { get; set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTimeOffset? RevokedOnUtc { get; set; }
    public string? RevokedByIp { get; set; }

    /// <summary>Hash of the token that replaced this one when rotated.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresOnUtc;
    public bool IsRevoked => RevokedOnUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
