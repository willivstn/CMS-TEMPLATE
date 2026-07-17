using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Modules;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Announcements;

// Sample module feature slice. Mirrors the structure of the core features so a new
// module is "just another vertical slice": DTOs, queries, commands and validators.

public sealed record AnnouncementListDto(Guid Id, string Title, bool IsPublished, DateTimeOffset? PublishedOnUtc, DateTimeOffset? ModifiedOnUtc);

public sealed record AnnouncementEditDto(Guid Id, string Title, string Body, bool IsPublished);

public sealed record GetAnnouncementsQuery : IRequest<IReadOnlyList<AnnouncementListDto>>;

public sealed class GetAnnouncementsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementListDto>>
{
    public async Task<IReadOnlyList<AnnouncementListDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken) =>
        await db.Announcements.AsNoTracking()
            .OrderByDescending(a => a.ModifiedOnUtc ?? a.CreatedOnUtc)
            .Select(a => new AnnouncementListDto(a.Id, a.Title, a.IsPublished, a.PublishedOnUtc, a.ModifiedOnUtc ?? a.CreatedOnUtc))
            .ToListAsync(cancellationToken);
}

public sealed record GetAnnouncementForEditQuery(Guid Id) : IRequest<AnnouncementEditDto?>;

public sealed class GetAnnouncementForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAnnouncementForEditQuery, AnnouncementEditDto?>
{
    public async Task<AnnouncementEditDto?> Handle(GetAnnouncementForEditQuery request, CancellationToken cancellationToken) =>
        await db.Announcements.AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new AnnouncementEditDto(a.Id, a.Title, a.Body, a.IsPublished))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed record CreateAnnouncementCommand(string Title, string Body, bool IsPublished) : IRequest<Result<Guid>>;

public sealed class CreateAnnouncementCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<CreateAnnouncementCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            IsPublished = request.IsPublished,
            PublishedOnUtc = request.IsPublished ? clock.UtcNow : null,
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(announcement.Id);
    }
}

public sealed record UpdateAnnouncementCommand(Guid Id, string Title, string Body, bool IsPublished) : IRequest<Result>;

public sealed class UpdateAnnouncementCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateAnnouncementCommand, Result>
{
    public async Task<Result> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(Error.NotFound("Announcement.NotFound", "Announcement not found."));
        }

        announcement.Title = request.Title;
        announcement.Body = request.Body;

        if (request.IsPublished && !announcement.IsPublished)
        {
            announcement.PublishedOnUtc = clock.UtcNow;
        }

        announcement.IsPublished = request.IsPublished;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAnnouncementCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteAnnouncementCommand, Result>
{
    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(Error.NotFound("Announcement.NotFound", "Announcement not found."));
        }

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class UpdateAnnouncementCommandValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty();
    }
}
