using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Content;
using CMS.Shared.Text;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Content;

/// <summary>Shared write-side helpers for content create/update/restore handlers.</summary>
internal static class ContentWrites
{
    /// <summary>
    /// Produces a unique slug within the tenant, deriving it from an explicit slug
    /// or the title and appending "-2", "-3"… on collision. The slug uniqueness
    /// index spans all content, so posts and pages share the namespace.
    /// </summary>
    public static async Task<string> UniqueSlugAsync(
        IApplicationDbContext db, string? providedSlug, string title, Domain.Enums.ContentType type,
        Guid? excludeId, CancellationToken cancellationToken)
    {
        var baseSlug = Slug.Generate(string.IsNullOrWhiteSpace(providedSlug) ? title : providedSlug);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "item";
        }

        var candidate = baseSlug;
        var suffix = 2;

        while (await db.ContentItems.AnyAsync(
                   c => c.Slug == candidate && (excludeId == null || c.Id != excludeId), cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        return candidate;
    }

    public static async Task ClearOtherHomePagesAsync(IApplicationDbContext db, Guid keepId, CancellationToken cancellationToken)
    {
        var others = await db.ContentItems
            .Where(c => c.IsHomePage && c.Id != keepId)
            .ToListAsync(cancellationToken);

        foreach (var other in others)
        {
            other.IsHomePage = false;
        }
    }

    /// <summary>Replaces a content item's tag links, creating any missing tags.</summary>
    public static async Task SyncTagsAsync(
        IApplicationDbContext db, ContentItem item, IReadOnlyList<string> tagNames, CancellationToken cancellationToken)
    {
        if (item.ContentTags.Count > 0)
        {
            db.ContentTags.RemoveRange(item.ContentTags);
            item.ContentTags.Clear();
        }

        var desired = tagNames
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .GroupBy(t => Slug.Generate(t), StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        if (desired.Count == 0)
        {
            return;
        }

        var slugs = desired.Select(t => Slug.Generate(t)).ToList();
        var existing = await db.Tags.Where(t => slugs.Contains(t.Slug)).ToDictionaryAsync(t => t.Slug, cancellationToken);

        foreach (var name in desired)
        {
            var slug = Slug.Generate(name);
            if (!existing.TryGetValue(slug, out var tag))
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = name, Slug = slug };
                db.Tags.Add(tag);
                existing[slug] = tag;
            }

            item.ContentTags.Add(new ContentTag { Id = Guid.NewGuid(), ContentItemId = item.Id, TagId = tag.Id });
        }
    }

    public static void AddVersion(
        IApplicationDbContext db, ContentItem item, int versionNumber, string note, string? createdBy, DateTimeOffset now)
    {
        db.ContentVersions.Add(new ContentVersion
        {
            Id = Guid.NewGuid(),
            ContentItemId = item.Id,
            VersionNumber = versionNumber,
            Title = item.Title,
            Summary = item.Summary,
            Body = item.Body,
            Status = item.Status,
            Note = note,
            CreatedOnUtc = now,
            CreatedBy = createdBy,
        });
    }
}
