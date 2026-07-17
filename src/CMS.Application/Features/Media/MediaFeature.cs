using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Domain.Entities.Media;
using CMS.Domain.Enums;
using CMS.Shared.Models;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Media;

// ---- DTOs ----------------------------------------------------------------

public sealed record MediaItemDto(
    Guid Id,
    string OriginalFileName,
    string? Url,
    string? ThumbnailUrl,
    MediaType Type,
    string ContentType,
    long SizeBytes,
    int? Width,
    int? Height,
    string? AltText,
    string? Title,
    DateTimeOffset CreatedOnUtc);

public sealed record MediaFolderDto(Guid Id, string Name, Guid? ParentId);

// ---- Queries -------------------------------------------------------------

public sealed record GetMediaItemsQuery(Guid? FolderId, string? Search, int Page = 1, int PageSize = 24)
    : IRequest<PagedResult<MediaItemDto>>;

public sealed class GetMediaItemsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMediaItemsQuery, PagedResult<MediaItemDto>>
{
    public async Task<PagedResult<MediaItemDto>> Handle(GetMediaItemsQuery request, CancellationToken cancellationToken)
    {
        var query = db.MediaItems.AsNoTracking().Where(m => m.FolderId == request.FolderId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(m => m.OriginalFileName.Contains(term) || (m.Title != null && m.Title.Contains(term)));
        }

        var projected = query
            .OrderByDescending(m => m.CreatedOnUtc)
            .Select(m => new MediaItemDto(
                m.Id, m.OriginalFileName, m.Url, m.ThumbnailUrl, m.Type, m.ContentType, m.SizeBytes,
                m.Width, m.Height, m.AltText, m.Title, m.CreatedOnUtc));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

public sealed record GetMediaFoldersQuery(Guid? ParentId) : IRequest<IReadOnlyList<MediaFolderDto>>;

public sealed class GetMediaFoldersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMediaFoldersQuery, IReadOnlyList<MediaFolderDto>>
{
    public async Task<IReadOnlyList<MediaFolderDto>> Handle(GetMediaFoldersQuery request, CancellationToken cancellationToken) =>
        await db.MediaFolders.AsNoTracking()
            .Where(f => f.ParentId == request.ParentId)
            .OrderBy(f => f.Name)
            .Select(f => new MediaFolderDto(f.Id, f.Name, f.ParentId))
            .ToListAsync(cancellationToken);
}

public sealed record GetMediaItemQuery(Guid Id) : IRequest<MediaItemDto?>;

public sealed class GetMediaItemQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMediaItemQuery, MediaItemDto?>
{
    public async Task<MediaItemDto?> Handle(GetMediaItemQuery request, CancellationToken cancellationToken) =>
        await db.MediaItems.AsNoTracking()
            .Where(m => m.Id == request.Id)
            .Select(m => new MediaItemDto(
                m.Id, m.OriginalFileName, m.Url, m.ThumbnailUrl, m.Type, m.ContentType, m.SizeBytes,
                m.Width, m.Height, m.AltText, m.Title, m.CreatedOnUtc))
            .FirstOrDefaultAsync(cancellationToken);
}

// ---- Commands ------------------------------------------------------------

/// <summary>Persists metadata for a file that has already been stored by the upload pipeline.</summary>
public sealed record CreateMediaItemCommand(
    Guid? FolderId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    string? Extension,
    long SizeBytes,
    MediaType Type,
    string StorageProvider,
    string StorageKey,
    string? Url,
    int? Width,
    int? Height,
    string? ThumbnailUrl,
    string? Checksum) : IRequest<Result<Guid>>;

public sealed class CreateMediaItemCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateMediaItemCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMediaItemCommand request, CancellationToken cancellationToken)
    {
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            FolderId = request.FolderId,
            FileName = request.FileName,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            Extension = request.Extension,
            SizeBytes = request.SizeBytes,
            Type = request.Type,
            StorageProvider = request.StorageProvider,
            StorageKey = request.StorageKey,
            Url = request.Url,
            Width = request.Width,
            Height = request.Height,
            ThumbnailUrl = request.ThumbnailUrl,
            Checksum = request.Checksum,
        };

        db.MediaItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(item.Id);
    }
}

public sealed record UpdateMediaItemCommand(Guid Id, string? AltText, string? Title) : IRequest<Result>;

public sealed class UpdateMediaItemCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateMediaItemCommand, Result>
{
    public async Task<Result> Handle(UpdateMediaItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Media.NotFound", "File not found."));
        }

        item.AltText = request.AltText;
        item.Title = request.Title;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteMediaItemCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteMediaItemCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteMediaItemCommand, Result>
{
    public async Task<Result> Handle(DeleteMediaItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("Media.NotFound", "File not found."));
        }

        // Soft delete keeps the physical file so an administrator can restore it.
        db.MediaItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record CreateMediaFolderCommand(string Name, Guid? ParentId) : IRequest<Result<Guid>>;

public sealed class CreateMediaFolderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateMediaFolderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateMediaFolderCommand request, CancellationToken cancellationToken)
    {
        var parentPath = "/";
        if (request.ParentId is { } parentId)
        {
            var parent = await db.MediaFolders.FirstOrDefaultAsync(f => f.Id == parentId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Guid>(Error.NotFound("Folder.NotFound", "Parent folder not found."));
            }

            parentPath = parent.Path;
        }

        var folder = new MediaFolder
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            Path = $"{parentPath}{request.Name.Trim()}/",
        };

        db.MediaFolders.Add(folder);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(folder.Id);
    }
}

public sealed record DeleteMediaFolderCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteMediaFolderCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteMediaFolderCommand, Result>
{
    public async Task<Result> Handle(DeleteMediaFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await db.MediaFolders.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (folder is null)
        {
            return Result.Failure(Error.NotFound("Folder.NotFound", "Folder not found."));
        }

        if (await db.MediaFolders.AnyAsync(f => f.ParentId == request.Id, cancellationToken) ||
            await db.MediaItems.AnyAsync(m => m.FolderId == request.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("Folder.NotEmpty", "Empty the folder before deleting it."));
        }

        db.MediaFolders.Remove(folder);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Validators ----------------------------------------------------------

public sealed class CreateMediaFolderCommandValidator : AbstractValidator<CreateMediaFolderCommand>
{
    public CreateMediaFolderCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
}
