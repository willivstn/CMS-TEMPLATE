using CMS.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Users;

/// <summary>A user's direct permission overrides, split into grants and denies.</summary>
public sealed record UserPermissionOverridesDto(
    Guid UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Grants,
    IReadOnlyList<string> Denies);

public sealed record GetUserPermissionOverridesQuery(Guid Id) : IRequest<UserPermissionOverridesDto?>;

public sealed class GetUserPermissionOverridesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserPermissionOverridesQuery, UserPermissionOverridesDto?>
{
    public async Task<UserPermissionOverridesDto?> Handle(GetUserPermissionOverridesQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.Id)
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var overrides = await db.UserPermissions.AsNoTracking()
            .Where(up => up.UserId == request.Id)
            .Select(up => new { up.Permission.Name, up.IsGranted })
            .ToListAsync(cancellationToken);

        return new UserPermissionOverridesDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            overrides.Where(o => o.IsGranted).Select(o => o.Name).ToList(),
            overrides.Where(o => !o.IsGranted).Select(o => o.Name).ToList());
    }
}
