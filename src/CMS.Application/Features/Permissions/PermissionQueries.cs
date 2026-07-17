using CMS.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Permissions;

/// <summary>The full permission catalogue grouped by area (for the viewer and matrices).</summary>
public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;

public sealed class GetPermissionCatalogQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionGroupDto>>
{
    public async Task<IReadOnlyList<PermissionGroupDto>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Group).ThenBy(p => p.Name)
            .Select(p => new { p.Group, p.Name, p.DisplayName, p.Description })
            .ToListAsync(cancellationToken);

        return permissions
            .GroupBy(p => p.Group)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionListItemDto(p.Name, p.DisplayName, p.Description)).ToList()))
            .ToList();
    }
}
