using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Entities.Access;
using CMS.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Roles;

// ---- Create --------------------------------------------------------------

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<Result<Guid>>;

public sealed class CreateRoleCommandHandler(IIdentityService identity)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken) =>
        identity.CreateRoleAsync(request.Name, request.Description, cancellationToken);
}

// ---- Update --------------------------------------------------------------

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest<Result>;

public sealed class UpdateRoleCommandHandler(IIdentityService identity)
    : IRequestHandler<UpdateRoleCommand, Result>
{
    public Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken) =>
        identity.UpdateRoleAsync(request.Id, request.Name, request.Description, cancellationToken);
}

// ---- Delete --------------------------------------------------------------

public sealed record DeleteRoleCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteRoleCommandHandler(IIdentityService identity)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    public Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken) =>
        identity.DeleteRoleAsync(request.Id, cancellationToken);
}

// ---- Set permissions (the matrix) ----------------------------------------

/// <summary>
/// Replaces the set of permissions granted to a role, then refreshes the security
/// stamp of every affected user so their permission claims re-issue on the next
/// request (which also updates their visible navigation).
/// </summary>
public sealed record SetRolePermissionsCommand(Guid Id, IReadOnlyCollection<string> Permissions) : IRequest<Result>;

public sealed class SetRolePermissionsCommandHandler(IApplicationDbContext db, IIdentityService identity)
    : IRequestHandler<SetRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.Id, cancellationToken);
        if (!roleExists)
        {
            return Result.Failure(Error.NotFound("Role.NotFound", "Role not found."));
        }

        var existing = await db.RolePermissions
            .Where(rp => rp.RoleId == request.Id)
            .ToListAsync(cancellationToken);
        db.RolePermissions.RemoveRange(existing);

        var permissionIds = await db.Permissions
            .Where(p => request.Permissions.Contains(p.Name))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in permissionIds)
        {
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = request.Id,
                PermissionId = permissionId,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await identity.RefreshSecurityStampsForRoleAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}
