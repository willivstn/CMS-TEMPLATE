using CMS.Shared.Results;

namespace CMS.Application.Identity;

/// <summary>Fields for creating a user account.</summary>
public sealed record CreateUserSpec(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

/// <summary>Editable profile fields for a user.</summary>
public sealed record UpdateUserSpec(
    Guid UserId,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);

/// <summary>
/// Wraps ASP.NET Core Identity write operations (user/role managers) behind a
/// Result-returning contract so CQRS handlers in the Application layer can perform
/// account management without depending on the Identity framework directly.
/// Implemented in the Web layer over <c>UserManager</c>/<c>RoleManager</c>.
/// </summary>
public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(CreateUserSpec spec, CancellationToken cancellationToken = default);

    Task<Result> UpdateUserAsync(UpdateUserSpec spec, CancellationToken cancellationToken = default);

    Task<Result> SetLockoutAsync(Guid userId, bool locked, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateRoleAsync(string name, string? description, CancellationToken cancellationToken = default);

    Task<Result> UpdateRoleAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken = default);

    Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Bumps the user's security stamp so their claims (incl. permissions) refresh.</summary>
    Task RefreshSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Bumps the security stamp of every user in a role (after permission changes).</summary>
    Task RefreshSecurityStampsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
