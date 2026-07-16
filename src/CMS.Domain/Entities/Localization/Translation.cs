using CMS.Domain.Common;

namespace CMS.Domain.Entities.Localization;

/// <summary>
/// A database-backed localized string. Complements resource files, letting
/// administrators add or override translations at runtime. Unique per
/// (<see cref="Key"/>, <see cref="LanguageCode"/>).
/// </summary>
public class Translation : Entity<Guid>
{
    /// <summary>Resource key, e.g. "Menu.Dashboard".</summary>
    public string Key { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;
    public Language Language { get; set; } = null!;

    public string Value { get; set; } = string.Empty;

    /// <summary>Optional grouping/resource name for the management UI.</summary>
    public string? Group { get; set; }
}
