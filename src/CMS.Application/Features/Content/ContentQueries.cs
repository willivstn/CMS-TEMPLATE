using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Domain.Enums;
using CMS.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Content;

// ---- List ----------------------------------------------------------------

public sealed record GetContentListQuery(
    ContentType Type,
    string? Search = null,
    ContentStatus? Status = null,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<ContentListItemDto>>;

public sealed class GetContentListQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContentListQuery, PagedResult<ContentListItemDto>>
{
    public async Task<PagedResult<ContentListItemDto>> Handle(GetContentListQuery request, CancellationToken cancellationToken)
    {
        var query = db.ContentItems.AsNoTracking().Where(c => c.Type == request.Type);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Title.Contains(term) || c.Slug.Contains(term));
        }

        if (request.Status is { } status)
        {
            query = query.Where(c => c.Status == status);
        }

        var projected = query
            .OrderByDescending(c => c.ModifiedOnUtc ?? c.CreatedOnUtc)
            .Select(c => new ContentListItemDto(
                c.Id,
                c.Title,
                c.Slug,
                c.Type,
                c.Status,
                c.Category != null ? c.Category.Name : null,
                c.Author != null ? c.Author.Email : null,
                c.IsHomePage,
                c.ViewCount,
                c.PublishedOnUtc,
                c.ModifiedOnUtc ?? c.CreatedOnUtc));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

// ---- For edit ------------------------------------------------------------

public sealed record GetContentForEditQuery(Guid Id) : IRequest<ContentEditDto?>;

public sealed class GetContentForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContentForEditQuery, ContentEditDto?>
{
    public async Task<ContentEditDto?> Handle(GetContentForEditQuery request, CancellationToken cancellationToken) =>
        await db.ContentItems.AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new ContentEditDto(
                c.Id, c.Type, c.Status, c.Title, c.Slug, c.Summary, c.Body,
                c.MetaTitle, c.MetaDescription, c.MetaKeywords, c.CanonicalUrl, c.OpenGraphImageUrl,
                c.CategoryId, c.FeaturedMediaId,
                c.FeaturedMedia != null ? c.FeaturedMedia.Url : null,
                c.IsHomePage, c.ScheduledOnUtc,
                c.ContentTags.Select(ct => ct.Tag.Name).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}

// ---- Preview -------------------------------------------------------------

public sealed record GetContentPreviewQuery(Guid Id) : IRequest<ContentPreviewDto?>;

public sealed class GetContentPreviewQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContentPreviewQuery, ContentPreviewDto?>
{
    public async Task<ContentPreviewDto?> Handle(GetContentPreviewQuery request, CancellationToken cancellationToken) =>
        await db.ContentItems.AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new ContentPreviewDto(
                c.Id, c.Type, c.Status, c.Title, c.Summary, c.Body,
                c.Author != null ? c.Author.Email : null,
                c.FeaturedMedia != null ? c.FeaturedMedia.Url : null,
                c.PublishedOnUtc))
            .FirstOrDefaultAsync(cancellationToken);
}

// ---- Versions ------------------------------------------------------------

public sealed record GetContentVersionsQuery(Guid Id) : IRequest<IReadOnlyList<ContentVersionDto>>;

public sealed class GetContentVersionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContentVersionsQuery, IReadOnlyList<ContentVersionDto>>
{
    public async Task<IReadOnlyList<ContentVersionDto>> Handle(GetContentVersionsQuery request, CancellationToken cancellationToken) =>
        await db.ContentVersions.AsNoTracking()
            .Where(v => v.ContentItemId == request.Id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ContentVersionDto(v.VersionNumber, v.Title, v.Status, v.Note, v.CreatedOnUtc, v.CreatedBy))
            .ToListAsync(cancellationToken);
}

// ---- Category options ----------------------------------------------------

public sealed record GetCategoryOptionsQuery : IRequest<IReadOnlyList<CategoryOptionDto>>;

public sealed class GetCategoryOptionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoryOptionsQuery, IReadOnlyList<CategoryOptionDto>>
{
    public async Task<IReadOnlyList<CategoryOptionDto>> Handle(GetCategoryOptionsQuery request, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOptionDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);
}
