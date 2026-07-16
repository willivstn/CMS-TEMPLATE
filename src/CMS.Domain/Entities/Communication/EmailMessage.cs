using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Communication;

/// <summary>
/// A queued outbound email. A background worker drains the queue, sends via the
/// configured provider, and records status/attempts for retry and diagnostics.
/// </summary>
public class EmailMessage : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Semicolon-separated recipient addresses.</summary>
    public string To { get; set; } = string.Empty;

    public string? Cc { get; set; }
    public string? Bcc { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }

    /// <summary>JSON array of storage keys for attachments, if any.</summary>
    public string? Attachments { get; set; }

    public EmailStatus Status { get; set; } = EmailStatus.Queued;

    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;

    public DateTimeOffset? ScheduledOnUtc { get; set; }
    public DateTimeOffset? SentOnUtc { get; set; }
    public string? Error { get; set; }

    public Guid? TemplateId { get; set; }
    public EmailTemplate? Template { get; set; }
}
