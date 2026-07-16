namespace CMS.Web.Models.Components;

/// <summary>
/// A reusable dashboard statistic tile. Rendered by <c>_StatCard.cshtml</c>.
/// <paramref name="Color"/> is a Bootstrap theme color name (primary, success…).
/// </summary>
public sealed record StatCard(string Title, string Value, string Icon, string Color, string? Url = null);
