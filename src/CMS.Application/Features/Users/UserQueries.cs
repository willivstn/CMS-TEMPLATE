using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Users;

// ---- List ----------------------------------------------------------------

/// <summary>Paged, searchable, filterable list of users.</summary>
public sealed record GetUsersQuery(
    string? Search = null,
    string? Role = null,
    bool? Active = null,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<UserListItemDto>>;

public sealed class GetUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.FirstName != null && u.FirstName.Contains(term)) ||
                (u.LastName != null && u.LastName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == request.Role));
        }

        if (request.Active is { } active)
        {
            query = query.Where(u => u.IsActive == active);
        }

        var projected = query
            .OrderByDescending(u => u.CreatedOnUtc)
            .Select(u => new UserListItemDto(
                u.Id,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.IsActive,
                u.LockoutEnd != null && u.LockoutEnd > now,
                u.EmailConfirmed,
                u.UserRoles.Select(ur => ur.Role.Name!).ToList(),
                u.CreatedOnUtc,
                u.LastLoginUtc));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

// ---- Details -------------------------------------------------------------

/// <summary>Full details for a single user, including effective permissions.</summary>
public sealed record GetUserDetailsQuery(Guid Id) : IRequest<UserDetailsDto?>;

public sealed class GetUserDetailsQueryHandler(IApplicationDbContext db, IPermissionService permissions)
    : IRequestHandler<GetUserDetailsQuery, UserDetailsDto?>
{
    public async Task<UserDetailsDto?> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.Id)
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.EmailConfirmed,
                u.TwoFactorEnabled,
                u.LockoutEnd,
                u.AccessFailedCount,
                u.CreatedOnUtc,
                u.LastLoginUtc,
                Roles = u.UserRoles.Select(ur => ur.Role.Name!).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var recentLogins = await db.LoginHistories.AsNoTracking()
            .Where(l => l.UserId == request.Id)
            .OrderByDescending(l => l.TimestampUtc)
            .Take(10)
            .Select(l => new LoginHistoryDto(l.Succeeded, l.FailureReason, l.IpAddress, l.TimestampUtc))
            .ToListAsync(cancellationToken);

        var effectivePermissions = (await permissions.GetPermissionsAsync(request.Id, cancellationToken))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        return new UserDetailsDto(
            user.Id, user.DisplayName, user.Email ?? string.Empty, user.FirstName, user.LastName,
            user.IsActive, user.EmailConfirmed, user.TwoFactorEnabled,
            user.LockoutEnd != null && user.LockoutEnd > now, user.LockoutEnd, user.AccessFailedCount,
            user.CreatedOnUtc, user.LastLoginUtc, user.Roles, effectivePermissions, recentLogins);
    }
}

// ---- For edit ------------------------------------------------------------

/// <summary>The editable projection of a user for the edit form.</summary>
public sealed record GetUserForEditQuery(Guid Id) : IRequest<UserEditDto?>;

public sealed class GetUserForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserForEditQuery, UserEditDto?>
{
    public async Task<UserEditDto?> Handle(GetUserForEditQuery request, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Id == request.Id)
            .Select(u => new UserEditDto(
                u.Id,
                u.Email ?? string.Empty,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.UserRoles.Select(ur => ur.Role.Name!).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}

// ---- Role options --------------------------------------------------------

/// <summary>All assignable roles (for create/edit forms).</summary>
public sealed record GetRoleOptionsQuery : IRequest<IReadOnlyList<RoleOptionDto>>;

public sealed class GetRoleOptionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRoleOptionsQuery, IReadOnlyList<RoleOptionDto>>
{
    public async Task<IReadOnlyList<RoleOptionDto>> Handle(GetRoleOptionsQuery request, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDto(r.Name!, r.Description))
            .ToListAsync(cancellationToken);
}
