using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.Models;

public sealed class LanguageFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(16)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Native name"), MaxLength(64)]
    public string? NativeName { get; set; }

    [Display(Name = "Right-to-left")]
    public bool IsRtl { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}

public sealed class TranslationFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(256)]
    public string Key { get; set; } = string.Empty;

    [Required, Display(Name = "Language")]
    public string LanguageCode { get; set; } = string.Empty;

    [Required, MaxLength(2048)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Group { get; set; }
}
