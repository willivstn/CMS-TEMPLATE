using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Domain.Enums;
using CMS.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Content;

/// <summary>A published content item as exposed by the public API.</summary>
public sealed record PublicContentDto(
    Guid Id,
    ContentType Type,
    string Title,
    string Slug,
    string? Summary,
    string? Body,
    string? Category,
    string? Author,
    IReadOnlyList<string> Tags,
    DateTimeOffset? PublishedOnUtc);

/// <summary>Paged list of published content for the public API.</summary>
public sealed record GetPublishedContentQuery(ContentType? Type, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PublicContentDto>>;

public sealed class GetPublishedContentQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPublishedContentQuery, PagedResult<PublicContentDto>>
{
    public async Task<PagedResult<PublicContentDto>> Handle(GetPublishedContentQuery request, CancellationToken cancellationToken)
    {
        var query = db.ContentItems.AsNoTracking().Where(c => c.Status == ContentStatus.Published);

        if (request.Type is { } type)
        {
            query = query.Where(c => c.Type == type);
        }

        return await query
            .OrderByDescending(c => c.PublishedOnUtc)
            .Select(c => new PublicContentDto(
                c.Id, c.Type, c.Title, c.Slug, c.Summary, c.Body,
                c.Category != null ? c.Category.Name : null,
                c.Author != null ? c.Author.Email : null,
                c.ContentTags.Select(ct => ct.Tag.Name).ToList(),
                c.PublishedOnUtc))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>A single published content item by slug.</summary>
public sealed record GetPublishedContentBySlugQuery(string Slug) : IRequest<PublicContentDto?>;

public sealed class GetPublishedContentBySlugQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPublishedContentBySlugQuery, PublicContentDto?>
{
    public async Task<PublicContentDto?> Handle(GetPublishedContentBySlugQuery request, CancellationToken cancellationToken) =>
        await db.ContentItems.AsNoTracking()
            .Where(c => c.Status == ContentStatus.Published && c.Slug == request.Slug)
            .Select(c => new PublicContentDto(
                c.Id, c.Type, c.Title, c.Slug, c.Summary, c.Body,
                c.Category != null ? c.Category.Name : null,
                c.Author != null ? c.Author.Email : null,
                c.ContentTags.Select(ct => ct.Tag.Name).ToList(),
                c.PublishedOnUtc))
            .FirstOrDefaultAsync(cancellationToken);
}
