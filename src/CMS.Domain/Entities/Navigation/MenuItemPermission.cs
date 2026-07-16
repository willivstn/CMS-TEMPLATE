using CMS.Domain.Common;
using CMS.Domain.Entities.Access;

namespace CMS.Domain.Entities.Navigation;

/// <summary>
/// Associates a menu item with a permission that grants visibility. When a menu
/// item has several, holding <em>any</em> of them reveals the item.
/// </summary>
public class MenuItemPermission : Entity<Guid>
{
    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
