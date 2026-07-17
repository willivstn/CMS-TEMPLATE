using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Content;
using CMS.Domain.Enums;
using CMS.Shared.Results;
using CMS.Shared.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Content;

/// <summary>Fields shared by create and update.</summary>
public interface IContentWrite
{
    string Title { get; }
    string? Slug { get; }
    string? Summary { get; }
    string? Body { get; }
    string? MetaTitle { get; }
    string? MetaDescription { get; }
    string? MetaKeywords { get; }
    string? CanonicalUrl { get; }
    string? OpenGraphImageUrl { get; }
    Guid? CategoryId { get; }
    Guid? FeaturedMediaId { get; }
    bool IsHomePage { get; }
    IReadOnlyList<string> Tags { get; }
}

// ---- Create --------------------------------------------------------------

public sealed record CreateContentCommand(
    ContentType Type,
    string Title,
    string? Slug,
    string? Summary,
    string? Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? OpenGraphImageUrl,
    Guid? CategoryId,
    Guid? FeaturedMediaId,
    bool IsHomePage,
    ContentStatus Status,
    DateTimeOffset? ScheduledOnUtc,
    IReadOnlyList<string> Tags) : IRequest<Result<Guid>>, IContentWrite;

public sealed class CreateContentCommandHandler(
    IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CreateContentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateContentCommand request, CancellationToken cancellationToken)
    {
        var slug = await ContentWrites.UniqueSlugAsync(db, request.Slug, request.Title, request.Type, null, cancellationToken);
        var now = clock.UtcNow;

        var item = new ContentItem
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Status = request.Status,
            Title = request.Title,
            Slug = slug,
            Summary = request.Summary,
            Body = request.Body,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            MetaKeywords = request.MetaKeywords,
            CanonicalUrl = request.CanonicalUrl,
            OpenGraphImageUrl = request.OpenGraphImageUrl,
            CategoryId = request.CategoryId,
            FeaturedMediaId = request.FeaturedMediaId,
            AuthorId = currentUser.UserId,
            IsHomePage = request.IsHomePage,
            PublishedOnUtc = request.Status == ContentStatus.Published ? now : null,
            ScheduledOnUtc = request.Status == ContentStatus.Scheduled ? request.ScheduledOnUtc : null,
        };

        db.ContentItems.Add(item);

        if (request.IsHomePage)
        {
            await ContentWrites.ClearOtherHomePagesAsync(db, item.Id, cancellationToken);
        }

        await ContentWrites.SyncTagsAsync(db, item, request.Tags, cancellationToken);
        ContentWrites.AddVersion(db, item, 1, "Created", currentUser.UserName, now);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(item.Id);
    }
}

// ---- Update --------------------------------------------------------------

public sealed record UpdateContentCommand(
    Guid Id,
    string Title,
    string? Slug,
    string? Summary,
    string? Body,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    string? OpenGraphImageUrl,
    Guid? CategoryId,
    Guid? FeaturedMediaId,
    bool IsHomePage,
    IReadOnlyList<string> Tags) : IRequest<Result>, IContentWrite;

public sealed class UpdateContentCommandHandler(
    IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<UpdateContentCommand, Result>
{
    public async Task<Result> Handle(UpdateContentCommand request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems
            .Include(c => c.ContentTags)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (item is null)
        {
            return Result.Failure(Error.NotFound("Content.NotFound", "Content not found."));
        }

        var now = clock.UtcNow;
        item.Slug = await ContentWrites.UniqueSlugAsync(db, request.Slug, request.Title, item.Type, item.Id, cancellationToken);
        item.Title = request.Title;
        item.Summary = request.Summary;
        item.Body = request.Body;
        item.MetaTitle = request.MetaTitle;
        item.MetaDescription = request.MetaDescription;
        item.MetaKeywords = request.MetaKeywords;
        item.CanonicalUrl = request.CanonicalUrl;
        item.OpenGraphImageUrl = request.OpenGraphImageUrl;
        item.CategoryId = request.CategoryId;
        item.FeaturedMediaId = request.FeaturedMediaId;
        item.IsHomePage = request.IsHomePage;

        if (request.IsHomePage)
        {
            await ContentWrites.ClearOtherHomePagesAsync(db, item.Id, cancellationToken);
        }

        await ContentWrites.SyncTagsAsync(db, item, request.Tags, cancellationToken);

        var nextVersion = await db.ContentVersions
            .Where(v => v.ContentItemId == item.Id)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        ContentWrites.AddVersion(db, item, nextVersion + 1, "Edited", currentUser.UserName, now);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Workflow transitions ------------------------------------------------

public sealed record PublishContentCommand(Guid Id) : IRequest<Result>;
public sealed record UnpublishContentCommand(Guid Id) : IRequest<Result>;
public sealed record ArchiveContentCommand(Guid Id) : IRequest<Result>;
public sealed record ScheduleContentCommand(Guid Id, DateTimeOffset When) : IRequest<Result>;

public sealed class ContentWorkflowHandler(IApplicationDbContext db, IDateTimeProvider clock) :
    IRequestHandler<PublishContentCommand, Result>,
    IRequestHandler<UnpublishContentCommand, Result>,
    IRequestHandler<ArchiveContentCommand, Result>,
    IRequestHandler<ScheduleContentCommand, Result>
{
    public Task<Result> Handle(PublishContentCommand request, CancellationToken cancellationToken) =>
        Transition(request.Id, item =>
        {
            item.Status = ContentStatus.Published;
            item.PublishedOnUtc ??= clock.UtcNow;
            item.ScheduledOnUtc = null;
            item.ArchivedOnUtc = null;
        }, cancellationToken);

    public Task<Result> Handle(UnpublishContentCommand request, CancellationToken cancellationToken) =>
        Transition(request.Id, item => item.Status = ContentStatus.Draft, cancellationToken);

    public Task<Result> Handle(ArchiveContentCommand request, CancellationToken cancellationToken) =>
        Transition(request.Id, item =>
        {
            item.Status = ContentStatus.Archived;
            item.ArchivedOnUtc = clock.UtcNow;
        }, cancellationToken);

    public Task<Result> Handle(ScheduleContentCommand request, CancellationToken cancellationToken) =>
        Transition(request.Id, item =>
        {
            item.Status = ContentStatus.Scheduled;
            item.ScheduledOnUtc = request.When;
        }, cancellationToken);

    private async Task<Result> Transition(Guid id, Action<ContentItem> apply, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Content.NotFound", "Content not found."));
        }

        apply(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Delete --------------------------------------------------------------

public sealed record DeleteContentCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteContentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteContentCommand, Result>
{
    public async Task<Result> Handle(DeleteContentCommand request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Content.NotFound", "Content not found."));
        }

        db.ContentItems.Remove(item); // soft-deleted by the interceptor
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Restore a version ---------------------------------------------------

public sealed record RestoreContentVersionCommand(Guid Id, int VersionNumber) : IRequest<Result>;

public sealed class RestoreContentVersionCommandHandler(
    IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<RestoreContentVersionCommand, Result>
{
    public async Task<Result> Handle(RestoreContentVersionCommand request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Content.NotFound", "Content not found."));
        }

        var version = await db.ContentVersions
            .FirstOrDefaultAsync(v => v.ContentItemId == request.Id && v.VersionNumber == request.VersionNumber, cancellationToken);
        if (version is null)
        {
            return Result.Failure(Error.NotFound("Content.VersionNotFound", "Version not found."));
        }

        item.Title = version.Title;
        item.Summary = version.Summary;
        item.Body = version.Body;

        var nextVersion = await db.ContentVersions
            .Where(v => v.ContentItemId == item.Id)
            .MaxAsync(v => v.VersionNumber, cancellationToken);
        ContentWrites.AddVersion(db, item, nextVersion + 1, $"Restored from v{request.VersionNumber}", currentUser.UserName, clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
