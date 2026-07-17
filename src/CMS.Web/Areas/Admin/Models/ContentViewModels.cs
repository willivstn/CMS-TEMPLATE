using System.ComponentModel.DataAnnotations;
using CMS.Application.Features.Content;
using CMS.Domain.Enums;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create/edit form for a page or post.</summary>
public sealed class ContentFormViewModel
{
    public Guid? Id { get; set; }

    public ContentType Type { get; set; } = ContentType.Page;

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(256)]
    [Display(Name = "Slug (URL)")]
    public string? Slug { get; set; }

    [MaxLength(1024)]
    public string? Summary { get; set; }

    public string? Body { get; set; }

    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "Tags (comma-separated)")]
    public string? Tags { get; set; }

    [Display(Name = "Featured image")]
    public Guid? FeaturedMediaId { get; set; }
    public string? FeaturedMediaUrl { get; set; }

    [Display(Name = "Set as home page")]
    public bool IsHomePage { get; set; }

    // Only used on create; edits change status via workflow actions.
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    // SEO
    [MaxLength(256)]
    public string? MetaTitle { get; set; }
    [MaxLength(512)]
    public string? MetaDescription { get; set; }
    [MaxLength(256)]
    public string? MetaKeywords { get; set; }
    [MaxLength(512)]
    public string? CanonicalUrl { get; set; }
    [MaxLength(512)]
    public string? OpenGraphImageUrl { get; set; }

    public IReadOnlyList<CategoryOptionDto> Categories { get; set; } = [];

    /// <summary>Splits the comma-separated tags box into names.</summary>
    public IReadOnlyList<string> TagList() =>
        string.IsNullOrWhiteSpace(Tags)
            ? []
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
