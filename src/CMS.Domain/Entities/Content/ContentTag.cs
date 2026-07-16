using CMS.Domain.Common;

namespace CMS.Domain.Entities.Content;

/// <summary>Join table implementing the many-to-many between content items and tags.</summary>
public class ContentTag : Entity<Guid>
{
    public Guid ContentItemId { get; set; }
    public ContentItem ContentItem { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
