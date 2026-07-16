using CMS.Domain.Common;

namespace CMS.Domain.Entities.Media;

/// <summary>
/// A folder in the media library. Self-references to form a tree and stores a
/// materialised <see cref="Path"/> for efficient breadcrumb and subtree queries.
/// </summary>
public class MediaFolder : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Materialised path, e.g. "/images/banners/", for fast lookups.</summary>
    public string Path { get; set; } = "/";

    public Guid? ParentId { get; set; }
    public MediaFolder? Parent { get; set; }
    public ICollection<MediaFolder> Children { get; set; } = [];

    public ICollection<MediaItem> Items { get; set; } = [];
}
