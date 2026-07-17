using System.ComponentModel.DataAnnotations;
using CMS.Application.Features.Categories;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create/edit form for a category.</summary>
public sealed class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    [Display(Name = "Slug (URL)")]
    public string? Slug { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [Display(Name = "Parent category")]
    public Guid? ParentId { get; set; }

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }

    public IReadOnlyList<CategoryParentOptionDto> Parents { get; set; } = [];
}

/// <summary>Editable metadata for a media asset.</summary>
public sealed class MediaMetaViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Alt text"), MaxLength(256)]
    public string? AltText { get; set; }

    [MaxLength(256)]
    public string? Title { get; set; }
}
