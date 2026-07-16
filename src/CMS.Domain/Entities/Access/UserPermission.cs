using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;

namespace CMS.Domain.Entities.Access;

/// <summary>
/// A direct, per-user permission override. <see cref="IsGranted"/> distinguishes
/// an explicit grant (adds a capability the user's roles don't provide) from an
/// explicit deny (removes one a role would otherwise grant). Denies win when the
/// effective permission set is computed.
/// </summary>
public class UserPermission : Entity<Guid>, IAuditable
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    /// <summary>True to explicitly grant, false to explicitly deny.</summary>
    public bool IsGranted { get; set; } = true;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
