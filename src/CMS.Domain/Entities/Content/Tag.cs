using CMS.Domain.Common;

namespace CMS.Domain.Entities.Content;

/// <summary>A free-form label attached to content items for cross-cutting grouping.</summary>
public class Tag : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly identifier, unique within a tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    public ICollection<ContentTag> ContentTags { get; set; } = [];
}
