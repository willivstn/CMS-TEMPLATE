using CMS.Domain.Common;

namespace CMS.Domain.Entities.Content;

/// <summary>
/// A hierarchical taxonomy node for organising content. Self-references via
/// <see cref="ParentId"/> to allow nested categories.
/// </summary>
public class Category : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly identifier, unique within a tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];

    public ICollection<ContentItem> ContentItems { get; set; } = [];
}
