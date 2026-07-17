using System.ComponentModel.DataAnnotations;
using CMS.Domain.Enums;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create/edit form for an email template.</summary>
public sealed class EmailTemplateFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Subject { get; set; } = string.Empty;

    [Required, Display(Name = "HTML body")]
    public string HtmlBody { get; set; } = string.Empty;

    [Display(Name = "Plain-text body")]
    public string? TextBody { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public string? AvailableTokens { get; set; }
}

/// <summary>Admin broadcast-notification form.</summary>
public sealed class NotificationBroadcastViewModel
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;

    [MaxLength(512)]
    public string? Link { get; set; }
}
