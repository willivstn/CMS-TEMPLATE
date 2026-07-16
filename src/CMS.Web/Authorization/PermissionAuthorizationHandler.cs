using CMS.Domain.Authorization;
using CMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;

namespace CMS.Web.Authorization;

/// <summary>
/// Satisfies a <see cref="PermissionRequirement"/> when the principal carries a
/// matching permission claim. Members of the Administrator role always pass, as a
/// safety net in case claims and grants have drifted.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User.IsInRole(DomainDefaults.AdministratorRole)
            || context.User.HasClaim(AppClaimTypes.Permission, requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
