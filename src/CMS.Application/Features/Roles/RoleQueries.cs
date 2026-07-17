using CMS.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Roles;

/// <summary>All roles with user and permission counts.</summary>
public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleListItemDto>>;

public sealed class GetRolesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleListItemDto>>
{
    public async Task<IReadOnlyList<RoleListItemDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemDto(
                r.Id,
                r.Name!,
                r.Description,
                r.IsSystem,
                r.UserRoles.Count,
                r.RolePermissions.Count))
            .ToListAsync(cancellationToken);
}

/// <summary>A role projected for the edit form.</summary>
public sealed record GetRoleForEditQuery(Guid Id) : IRequest<RoleEditDto?>;

public sealed class GetRoleForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRoleForEditQuery, RoleEditDto?>
{
    public async Task<RoleEditDto?> Handle(GetRoleForEditQuery request, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(r => new RoleEditDto(r.Id, r.Name!, r.Description, r.IsSystem))
            .FirstOrDefaultAsync(cancellationToken);
}

/// <summary>A role plus its currently-granted permission names (for the matrix).</summary>
public sealed record GetRolePermissionsQuery(Guid Id) : IRequest<RolePermissionsDto?>;

public sealed class GetRolePermissionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolePermissionsQuery, RolePermissionsDto?>
{
    public async Task<RolePermissionsDto?> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(r => new RolePermissionsDto(
                r.Id,
                r.Name!,
                r.IsSystem,
                r.RolePermissions.Select(rp => rp.Permission.Name).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
