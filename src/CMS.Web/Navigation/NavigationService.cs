using System.Text.Json;
using CMS.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Web.Navigation;

/// <summary>
/// Assembles the navigation for the current request. The raw menu (small, rarely
/// changed) is cached application-wide; per-request work is limited to permission
/// filtering, URL resolution and active-branch detection. Results are memoised for
/// the lifetime of the (scoped) service so the menu and breadcrumb view components
/// share one build.
/// </summary>
public sealed class NavigationService(
    IApplicationDbContext db,
    IMemoryCache cache,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator) : INavigationService
{
    private const string CacheKey = "nav:menu-items";
    private IReadOnlyList<MenuNodeViewModel>? _roots;

    private sealed record MenuItemData(
        Guid Id, Guid? ParentId, string Title, string? Icon, string? Area, string? Controller,
        string? Action, string? RouteValues, string? Url, int SortOrder, bool IsEnabled,
        bool IsExternal, bool OpenInNewTab, string[] Permissions);

    public async Task<IReadOnlyList<MenuNodeViewModel>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        if (_roots is not null)
        {
            return _roots;
        }

        var items = await GetCachedItemsAsync(cancellationToken);
        var byParent = items.ToLookup(i => i.ParentId);
        _roots = Build(byParent, null, byParent);
        return _roots;
    }

    public async Task<IReadOnlyList<MenuNodeViewModel>> GetBreadcrumbsAsync(CancellationToken cancellationToken = default)
    {
        var roots = await GetMenuAsync(cancellationToken);
        var trail = new List<MenuNodeViewModel>();
        CollectActive(roots, trail);
        return trail;
    }

    private static void CollectActive(IReadOnlyList<MenuNodeViewModel> nodes, List<MenuNodeViewModel> trail)
    {
        var active = nodes.FirstOrDefault(n => n.IsActive);
        if (active is null)
        {
            return;
        }

        trail.Add(active);
        CollectActive(active.Children, trail);
    }

    private async Task<List<MenuItemData>> GetCachedItemsAsync(CancellationToken cancellationToken)
    {
        var cached = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await db.MenuItems
                .Where(m => m.IsVisible)
                .OrderBy(m => m.SortOrder)
                .Select(m => new MenuItemData(
                    m.Id, m.ParentId, m.Title, m.Icon, m.Area, m.Controller, m.Action,
                    m.RouteValues, m.Url, m.SortOrder, m.IsEnabled, m.IsExternal, m.OpenInNewTab,
                    m.MenuPermissions.Select(mp => mp.Permission.Name).ToArray()))
                .ToListAsync(cancellationToken);
        });

        return cached ?? [];
    }

    private List<MenuNodeViewModel> Build(
        ILookup<Guid?, MenuItemData> byParent, Guid? parentId, ILookup<Guid?, MenuItemData> all)
    {
        var nodes = new List<MenuNodeViewModel>();

        foreach (var item in byParent[parentId].OrderBy(i => i.SortOrder))
        {
            if (!IsPermitted(item))
            {
                continue;
            }

            var children = Build(byParent, item.Id, all);
            var isGroup = all[item.Id].Any();

            // Group headers appear only when at least one child survived filtering.
            if (isGroup && children.Count == 0)
            {
                continue;
            }

            var url = ResolveUrl(item);
            var node = new MenuNodeViewModel
            {
                Title = item.Title,
                Icon = item.Icon,
                Url = url,
                IsExternal = item.IsExternal,
                OpenInNewTab = item.OpenInNewTab,
                IsEnabled = item.IsEnabled,
                Children = children,
            };

            node.IsActive = IsCurrent(item) || children.Any(c => c.IsActive);
            nodes.Add(node);
        }

        return nodes;
    }

    private bool IsPermitted(MenuItemData item) =>
        item.Permissions.Length == 0 || item.Permissions.Any(currentUser.HasPermission);

    private string? ResolveUrl(MenuItemData item)
    {
        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            return item.Url;
        }

        if (string.IsNullOrWhiteSpace(item.Controller) || string.IsNullOrWhiteSpace(item.Action))
        {
            return null;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        var values = ParseRouteValues(item.RouteValues);
        values["area"] = item.Area ?? string.Empty;

        return linkGenerator.GetPathByAction(httpContext, item.Action, item.Controller, values);
    }

    private bool IsCurrent(MenuItemData item)
    {
        var routeValues = httpContextAccessor.HttpContext?.Request.RouteValues;
        if (routeValues is null || string.IsNullOrWhiteSpace(item.Controller))
        {
            return false;
        }

        return Matches(routeValues, "area", item.Area)
            && Matches(routeValues, "controller", item.Controller)
            && Matches(routeValues, "action", item.Action);
    }

    private static bool Matches(RouteValueDictionary routeValues, string key, string? expected)
    {
        var current = routeValues.TryGetValue(key, out var value) ? value?.ToString() : null;

        if (string.IsNullOrEmpty(expected))
        {
            return string.IsNullOrEmpty(current);
        }

        return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> ParseRouteValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            return parsed is null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
