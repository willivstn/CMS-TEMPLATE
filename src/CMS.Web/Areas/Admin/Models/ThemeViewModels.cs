using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create/edit form for a custom theme.</summary>
public sealed class ThemeFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Dark base")]
    public bool IsDark { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    [Display(Name = "CSS variables (JSON)"), MaxLength(4000)]
    public string? Variables { get; set; }

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}
