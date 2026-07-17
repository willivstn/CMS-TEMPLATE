using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create/edit form for the sample Announcements module.</summary>
public sealed class AnnouncementFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Published")]
    public bool IsPublished { get; set; }
}
