using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Entities.Access;
using CMS.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Users;

// ---- Create --------------------------------------------------------------

public sealed record CreateUserCommand(
    string Email,
    string? FirstName,
    string? LastName,
    string Password,
    bool IsActive,
    IReadOnlyCollection<string> Roles) : IRequest<Result<Guid>>;

public sealed class CreateUserCommandHandler(IIdentityService identity)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        identity.CreateUserAsync(
            new CreateUserSpec(request.Email, request.Password, request.FirstName, request.LastName,
                request.IsActive, request.Roles),
            cancellationToken);
}

// ---- Update (profile + roles + active) -----------------------------------

public sealed record UpdateUserCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IReadOnlyCollection<string> Roles) : IRequest<Result>;

public sealed class UpdateUserCommandHandler(IIdentityService identity)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken) =>
        identity.UpdateUserAsync(
            new UpdateUserSpec(request.Id, request.FirstName, request.LastName, request.IsActive, request.Roles),
            cancellationToken);
}

// ---- Lockout -------------------------------------------------------------

public sealed record SetUserLockoutCommand(Guid Id, bool Locked) : IRequest<Result>;

public sealed class SetUserLockoutCommandHandler(IIdentityService identity)
    : IRequestHandler<SetUserLockoutCommand, Result>
{
    public Task<Result> Handle(SetUserLockoutCommand request, CancellationToken cancellationToken) =>
        identity.SetLockoutAsync(request.Id, request.Locked, cancellationToken);
}

// ---- Reset password ------------------------------------------------------

public sealed record ResetUserPasswordCommand(Guid Id, string NewPassword) : IRequest<Result>;

public sealed class ResetUserPasswordCommandHandler(IIdentityService identity)
    : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) =>
        identity.ResetPasswordAsync(request.Id, request.NewPassword, cancellationToken);
}

// ---- Delete --------------------------------------------------------------

public sealed record DeleteUserCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteUserCommandHandler(IIdentityService identity)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken) =>
        identity.DeleteUserAsync(request.Id, cancellationToken);
}

// ---- Per-user permission overrides ---------------------------------------

/// <summary>
/// Replaces a user's direct permission overrides. <paramref name="Grants"/> add
/// capabilities beyond the user's roles; <paramref name="Denies"/> remove ones a
/// role would otherwise grant. Refreshes the security stamp so claims re-issue.
/// </summary>
public sealed record SetUserPermissionsCommand(
    Guid Id,
    IReadOnlyCollection<string> Grants,
    IReadOnlyCollection<string> Denies) : IRequest<Result>;

public sealed class SetUserPermissionsCommandHandler(IApplicationDbContext db, IIdentityService identity)
    : IRequestHandler<SetUserPermissionsCommand, Result>
{
    public async Task<Result> Handle(SetUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.UserPermissions
            .Where(up => up.UserId == request.Id)
            .ToListAsync(cancellationToken);
        db.UserPermissions.RemoveRange(existing);

        var names = request.Grants.Concat(request.Denies).Distinct().ToList();
        var permissionIds = await db.Permissions
            .Where(p => names.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id, cancellationToken);

        foreach (var name in request.Grants)
        {
            if (permissionIds.TryGetValue(name, out var pid))
            {
                db.UserPermissions.Add(new UserPermission { Id = Guid.NewGuid(), UserId = request.Id, PermissionId = pid, IsGranted = true });
            }
        }

        foreach (var name in request.Denies)
        {
            if (permissionIds.TryGetValue(name, out var pid))
            {
                db.UserPermissions.Add(new UserPermission { Id = Guid.NewGuid(), UserId = request.Id, PermissionId = pid, IsGranted = false });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await identity.RefreshSecurityStampAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}
