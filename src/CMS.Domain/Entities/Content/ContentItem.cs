using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;
using CMS.Domain.Entities.Media;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Content;

/// <summary>
/// The core CMS content aggregate, covering both pages and posts (distinguished by
/// <see cref="Type"/>). Carries editorial workflow (<see cref="Status"/>), SEO
/// metadata, friendly URL (<see cref="Slug"/>), scheduling, and a full revision
/// history via <see cref="Versions"/>. Pages may be nested through
/// <see cref="ParentId"/> to form a site tree.
/// </summary>
public class ContentItem : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public ContentType Type { get; set; } = ContentType.Page;
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public string Title { get; set; } = string.Empty;

    /// <summary>Friendly URL segment, unique within a tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Summary { get; set; }

    /// <summary>Rich-text/HTML body produced by the editor.</summary>
    public string? Body { get; set; }

    // --- SEO ---
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OpenGraphImageUrl { get; set; }

    // --- Relationships ---
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    public Guid? FeaturedMediaId { get; set; }
    public MediaItem? FeaturedMedia { get; set; }

    /// <summary>Parent page for hierarchical page trees (null for posts / top-level).</summary>
    public Guid? ParentId { get; set; }
    public ContentItem? Parent { get; set; }
    public ICollection<ContentItem> Children { get; set; } = [];

    // --- Scheduling / stats ---
    public DateTimeOffset? PublishedOnUtc { get; set; }
    public DateTimeOffset? ScheduledOnUtc { get; set; }
    public DateTimeOffset? ArchivedOnUtc { get; set; }

    public bool IsHomePage { get; set; }
    public long ViewCount { get; set; }

    public ICollection<ContentTag> ContentTags { get; set; } = [];
    public ICollection<ContentVersion> Versions { get; set; } = [];
}
