using CMS.Domain.Common;

namespace CMS.Domain.Entities.Navigation;

/// <summary>
/// A database-driven navigation node supporting unlimited nesting via
/// <see cref="ParentId"/>. A menu item may target an MVC action
/// (<see cref="Controller"/>/<see cref="Action"/>/<see cref="Area"/>) or an
/// explicit <see cref="Url"/>. Visibility is further gated by the permissions in
/// <see cref="MenuPermissions"/> so users never see links they cannot use.
/// </summary>
public class MenuItem : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];

    public string Title { get; set; } = string.Empty;

    /// <summary>Icon CSS class (e.g. a Bootstrap Icons class) rendered before the title.</summary>
    public string? Icon { get; set; }

    public string? Area { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }

    /// <summary>Optional route values serialized as JSON, e.g. {"id":5}.</summary>
    public string? RouteValues { get; set; }

    /// <summary>Explicit URL; takes precedence over controller/action when set.</summary>
    public string? Url { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsExternal { get; set; }
    public bool OpenInNewTab { get; set; }

    /// <summary>Permissions controlling visibility; empty means visible to all authenticated users.</summary>
    public ICollection<MenuItemPermission> MenuPermissions { get; set; } = [];
}
