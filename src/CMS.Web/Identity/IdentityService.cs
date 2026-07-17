using CMS.Application.Identity;
using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;
using CMS.Shared.Results;
using Microsoft.AspNetCore.Identity;

namespace CMS.Web.Identity;

/// <summary>
/// Implements <see cref="IIdentityService"/> over ASP.NET Core Identity. Lives in
/// the Web layer (Identity is web-coupled) and returns <see cref="Result"/> values
/// so Application handlers stay framework-agnostic. Soft delete is handled by the
/// SaveChanges interceptor: <c>UserManager.DeleteAsync</c> is rewritten to a flag
/// update because <see cref="ApplicationUser"/> is soft-deletable.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IIdentityService
{
    public async Task<Result<Guid>> CreateUserAsync(CreateUserSpec spec, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = spec.Email,
            Email = spec.Email,
            FirstName = spec.FirstName,
            LastName = spec.LastName,
            IsActive = spec.IsActive,
            EmailConfirmed = true, // admin-created accounts are pre-confirmed
        };

        var created = await userManager.CreateAsync(user, spec.Password);
        if (!created.Succeeded)
        {
            return Result.Failure<Guid>(ToError(created));
        }

        var roles = await FilterExistingRolesAsync(spec.Roles);
        if (roles.Count > 0)
        {
            await userManager.AddToRolesAsync(user, roles);
        }

        return Result.Success(user.Id);
    }

    public async Task<Result> UpdateUserAsync(UpdateUserSpec spec, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(spec.UserId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = spec.FirstName;
        user.LastName = spec.LastName;
        user.IsActive = spec.IsActive;

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return Result.Failure(ToError(updated));
        }

        var desired = await FilterExistingRolesAsync(spec.Roles);
        var current = await userManager.GetRolesAsync(user);
        var toAdd = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, toRemove);
        }

        if (toAdd.Count > 0)
        {
            await userManager.AddToRolesAsync(user, toAdd);
        }

        // Roles/status changed — refresh claims (and sign out deactivated users).
        await userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }

    public async Task<Result> SetLockoutAsync(Guid userId, bool locked, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        if (locked)
        {
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == DomainDefaults.AdministratorUserId)
        {
            return Result.Failure(Error.Conflict("User.Protected", "The built-in administrator cannot be deleted."));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        // Invalidate existing sessions, then soft-delete (interceptor rewrites the delete).
        await userManager.UpdateSecurityStampAsync(user);
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result<Guid>> CreateRoleAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = new ApplicationRole(name) { Id = Guid.NewGuid(), Description = description };
        var result = await roleManager.CreateAsync(role);
        return result.Succeeded ? Result.Success(role.Id) : Result.Failure<Guid>(ToError(result));
    }

    public async Task<Result> UpdateRoleAsync(Guid roleId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return NotFound();
        }

        role.Description = description;

        // Built-in role names are protected; only their description can change.
        if (!role.IsSystem && !string.Equals(role.Name, name, StringComparison.Ordinal))
        {
            await roleManager.SetRoleNameAsync(role, name);
        }

        var result = await roleManager.UpdateAsync(role);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return NotFound();
        }

        if (role.IsSystem)
        {
            return Result.Failure(Error.Conflict("Role.Protected", "Built-in roles cannot be deleted."));
        }

        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task RefreshSecurityStampAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
    }

    public async Task RefreshSecurityStampsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role?.Name is null)
        {
            return;
        }

        foreach (var user in await userManager.GetUsersInRoleAsync(role.Name))
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
    }

    private async Task<List<string>> FilterExistingRolesAsync(IReadOnlyCollection<string> roles)
    {
        var result = new List<string>();
        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                result.Add(role);
            }
        }

        return result;
    }

    private static Result NotFound() => Result.Failure(Error.NotFound("User.NotFound", "The account was not found."));

    private static Error ToError(IdentityResult result) =>
        Error.Validation("Identity.Failed", string.Join(" ", result.Errors.Select(e => e.Description)));
}
