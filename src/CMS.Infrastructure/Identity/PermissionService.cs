using CMS.Application.Common.Abstractions;
using CMS.Domain.Authorization;
using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Identity;

/// <summary>
/// Computes effective permissions from the database. Administrators short-circuit
/// to the full catalogue so newly-added permissions apply without re-seeding
/// grants. Results feed the claims factory at sign-in.
/// </summary>
public sealed class PermissionService(AppDbContext db) : IPermissionService
{
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await db.Set<ApplicationUserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var roleNames = await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);

        // Administrator implicitly holds every permission.
        if (roleNames.Contains(DomainDefaults.AdministratorRole))
        {
            return PermissionRegistry.All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        }

        var rolePermissions = await db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .ToListAsync(cancellationToken);

        var grants = await db.UserPermissions
            .Where(up => up.UserId == userId && up.IsGranted)
            .Select(up => up.Permission.Name)
            .ToListAsync(cancellationToken);

        var denies = await db.UserPermissions
            .Where(up => up.UserId == userId && !up.IsGranted)
            .Select(up => up.Permission.Name)
            .ToListAsync(cancellationToken);

        var effective = rolePermissions
            .Union(grants, StringComparer.Ordinal)
            .Except(denies, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        return effective;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }
}
