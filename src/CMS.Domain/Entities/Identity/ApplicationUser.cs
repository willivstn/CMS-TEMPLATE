using CMS.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CMS.Domain.Entities.Identity;

/// <summary>
/// Application user. Extends ASP.NET Core Identity with profile fields, audit and
/// soft-delete. Users are global (not tenant-scoped) so authentication can occur
/// before a tenant is resolved; per-tenant access is layered on separately.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDeletable
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>Convenience display name; falls back to the user name.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? UserName ?? Email ?? Id.ToString()
            : $"{FirstName} {LastName}".Trim();

    public string? AvatarUrl { get; set; }

    /// <summary>IANA time-zone id used to localise timestamps for this user.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Preferred UI language (BCP-47 code), e.g. "en-US".</summary>
    public string? LanguageCode { get; set; }

    /// <summary>When false the account cannot sign in regardless of lockout state.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginUtc { get; set; }

    // Navigation
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
    public ICollection<Access.UserPermission> UserPermissions { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<LoginHistory> LoginHistories { get; set; } = [];

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
    public string? ModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
}
