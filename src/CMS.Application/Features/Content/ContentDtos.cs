using CMS.Domain.Enums;

namespace CMS.Application.Features.Content;

/// <summary>A row in the pages/posts list.</summary>
public sealed record ContentListItemDto(
    Guid Id,
    string Title,
    string Slug,
    ContentType Type,
    ContentStatus Status,
    string? Category,
    string? Author,
    bool IsHomePage,
    long ViewCount,
    DateTimeOffset? PublishedOnUtc,
    DateTimeOffset? ModifiedOnUtc);

/// <summary>The editable shape of a content item.</summary>
public sealed record ContentEditDto(
    Guid Id,
    ContentType Type,
    ContentStatus Status,
    string Title,
    string Slug,
    string? Summary,
    string? Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? OpenGraphImageUrl,
    Guid? CategoryId,
    Guid? FeaturedMediaId,
    string? FeaturedMediaUrl,
    bool IsHomePage,
    DateTimeOffset? ScheduledOnUtc,
    IReadOnlyList<string> Tags);

/// <summary>Read-only content for the preview page.</summary>
public sealed record ContentPreviewDto(
    Guid Id,
    ContentType Type,
    ContentStatus Status,
    string Title,
    string? Summary,
    string? Body,
    string? Author,
    string? FeaturedMediaUrl,
    DateTimeOffset? PublishedOnUtc);

/// <summary>A single revision in the version history.</summary>
public sealed record ContentVersionDto(
    int VersionNumber,
    string Title,
    ContentStatus Status,
    string? Note,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);

/// <summary>A selectable category option.</summary>
public sealed record CategoryOptionDto(Guid Id, string Name);
