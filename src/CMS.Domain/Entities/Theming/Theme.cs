using CMS.Domain.Common;

namespace CMS.Domain.Entities.Theming;

/// <summary>
/// A selectable UI theme. Ships with Light and Dark; administrators can add
/// custom themes whose <see cref="Variables"/> override CSS custom properties.
/// Tenants may pin a default theme via <c>Tenant.ThemeKey</c>.
/// </summary>
public class Theme : AuditableEntity
{
    /// <summary>Stable key used in cookies/markup, e.g. "light", "dark".</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this theme is a dark variant (affects the toggle icon).</summary>
    public bool IsDark { get; set; }

    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON map of CSS custom-property overrides, e.g. {"--bs-primary":"#0d6efd"}.</summary>
    public string? Variables { get; set; }

    public int SortOrder { get; set; }
}
