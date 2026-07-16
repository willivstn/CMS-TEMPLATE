using CMS.Domain.Common;

namespace CMS.Domain.Entities.Localization;

/// <summary>
/// A UI language available to the application. Drives the language switcher and,
/// via <see cref="IsRtl"/>, right-to-left layout support.
/// </summary>
public class Language : AuditableEntity
{
    /// <summary>BCP-47 culture code, e.g. "en-US". Unique.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>English name, e.g. "English".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Endonym, e.g. "العربية".</summary>
    public string? NativeName { get; set; }

    public bool IsRtl { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Optional flag icon class or emoji for the switcher.</summary>
    public string? FlagIcon { get; set; }

    public int SortOrder { get; set; }

    public ICollection<Translation> Translations { get; set; } = [];
}
