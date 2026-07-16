using CMS.Domain.Common;

namespace CMS.Domain.Entities.Access;

/// <summary>
/// A single, granular capability such as "Content.Edit". Roles are granted
/// permissions via <see cref="RolePermission"/>; individual users may be granted
/// or denied permissions directly via <see cref="UserPermission"/>.
/// </summary>
public class Permission : Entity<Guid>
{
    /// <summary>The stable identifier, e.g. "Content.Edit". Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Logical group for UI, e.g. "Content".</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Human-friendly label shown in the permission matrix.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>System permissions ship with the template and cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
