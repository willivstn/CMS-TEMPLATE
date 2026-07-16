using CMS.Domain.Common;
using CMS.Domain.Entities.Access;
using Microsoft.AspNetCore.Identity;

namespace CMS.Domain.Entities.Identity;

/// <summary>
/// A role — a named bundle of permissions. Extends Identity with a description,
/// an <see cref="IsSystem"/> flag protecting built-in roles from deletion, and
/// the permission assignments that give the role its capabilities.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>, IAuditable
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }

    /// <summary>Built-in roles cannot be renamed or deleted through the UI.</summary>
    public bool IsSystem { get; set; }

    // Navigation
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
