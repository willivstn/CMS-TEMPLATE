using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Content;

/// <summary>
/// An immutable snapshot of a <see cref="ContentItem"/> captured on each save,
/// enabling revision history, diffing, and rollback.
/// </summary>
public class ContentVersion : Entity<Guid>
{
    public Guid ContentItemId { get; set; }
    public ContentItem ContentItem { get; set; } = null!;

    /// <summary>Monotonic version number, starting at 1 per content item.</summary>
    public int VersionNumber { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public ContentStatus Status { get; set; }

    /// <summary>Optional change note describing this revision.</summary>
    public string? Note { get; set; }

    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
}
