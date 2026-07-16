namespace CMS.Web.Navigation;

/// <summary>
/// A resolved, permission-filtered navigation node ready for rendering. Built by
/// <see cref="INavigationService"/> from the database-driven menu.
/// </summary>
public sealed class MenuNodeViewModel
{
    public required string Title { get; init; }
    public string? Icon { get; init; }

    /// <summary>The resolved href; null for group headers that only expand children.</summary>
    public string? Url { get; init; }

    public bool IsExternal { get; init; }
    public bool OpenInNewTab { get; init; }
    public bool IsEnabled { get; init; } = true;

    /// <summary>True when this node (or a descendant) matches the current request.</summary>
    public bool IsActive { get; set; }

    public IReadOnlyList<MenuNodeViewModel> Children { get; init; } = [];

    public bool HasChildren => Children.Count > 0;
}
