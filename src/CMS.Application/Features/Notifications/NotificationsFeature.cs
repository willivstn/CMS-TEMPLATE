using CMS.Application.Common.Abstractions;
using CMS.Domain.Enums;
using CMS.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Notifications;

public sealed record NotificationDto(Guid Id, string Title, string Message, NotificationType Type, string? Link, bool IsRead, DateTimeOffset CreatedOnUtc);

// ---- Queries -------------------------------------------------------------

public sealed record GetUserNotificationsQuery(Guid UserId, int Take = 20) : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class GetUserNotificationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken) =>
        await db.NotificationRecipients.AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.Notification.CreatedOnUtc)
            .Take(request.Take)
            .Select(r => new NotificationDto(
                r.Notification.Id, r.Notification.Title, r.Notification.Message, r.Notification.Type,
                r.Notification.Link, r.IsRead, r.Notification.CreatedOnUtc))
            .ToListAsync(cancellationToken);
}

public sealed record GetUnreadNotificationCountQuery(Guid UserId) : IRequest<int>;

public sealed class GetUnreadNotificationCountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken) =>
        await db.NotificationRecipients.AsNoTracking()
            .CountAsync(r => r.UserId == request.UserId && !r.IsRead, cancellationToken);
}

// ---- Commands ------------------------------------------------------------

public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var recipient = await db.NotificationRecipients
            .FirstOrDefaultAsync(r => r.UserId == request.UserId && r.NotificationId == request.NotificationId, cancellationToken);

        if (recipient is { IsRead: false })
        {
            recipient.IsRead = true;
            recipient.ReadOnUtc = clock.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result>;

public sealed class MarkAllNotificationsReadCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await db.NotificationRecipients
            .Where(r => r.UserId == request.UserId && !r.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var recipient in unread)
        {
            recipient.IsRead = true;
            recipient.ReadOnUtc = clock.UtcNow;
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
