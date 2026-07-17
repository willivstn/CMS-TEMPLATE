using CMS.Domain.Common;

namespace CMS.Domain.Entities.Modules;

/// <summary>
/// Sample module entity. "Announcements" demonstrates how a new feature plugs into
/// the template: it is an auditable, tenant-scoped, soft-deletable entity like any
/// core entity, and its slice adds permissions, a menu entry, CQRS handlers and an
/// admin UI without modifying the core. See docs/extending.md.
/// </summary>
public class Announcement : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedOnUtc { get; set; }
}
