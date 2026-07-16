namespace CMS.Web.Navigation;

/// <summary>
/// Builds the current user's navigation from the database-driven menu: assembles
/// the nested tree, filters out items the user lacks permission for, resolves
/// URLs, flags the active branch, and derives breadcrumbs for the current request.
/// </summary>
public interface INavigationService
{
    /// <summary>The permission-filtered root menu nodes, with the active branch flagged.</summary>
    Task<IReadOnlyList<MenuNodeViewModel>> GetMenuAsync(CancellationToken cancellationToken = default);

    /// <summary>The breadcrumb trail (root → current) for the current request.</summary>
    Task<IReadOnlyList<MenuNodeViewModel>> GetBreadcrumbsAsync(CancellationToken cancellationToken = default);
}
