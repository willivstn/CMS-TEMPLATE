using CMS.Domain.Common;

namespace CMS.Domain.Entities.Communication;

/// <summary>
/// A reusable, tokenized email template (e.g. "Welcome", "ResetPassword").
/// Bodies contain placeholder tokens such as {{UserName}} substituted at send time.
/// </summary>
public class EmailTemplate : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Unique key used to look the template up, e.g. "ResetPassword".</summary>
    public string Key { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public string? TextBody { get; set; }

    public string? Description { get; set; }

    /// <summary>Comma-separated list of supported tokens, for the editor UI.</summary>
    public string? AvailableTokens { get; set; }

    public bool IsActive { get; set; } = true;
}
