using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;

namespace CMS.Domain.Entities.Access;

/// <summary>
/// Grants a permission to a role. The presence of a row means "allowed"; roles
/// only ever add capabilities. Denials are expressed per-user via
/// <see cref="UserPermission"/>.
/// </summary>
public class RolePermission : Entity<Guid>, IAuditable
{
    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
