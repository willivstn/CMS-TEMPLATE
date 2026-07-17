using CMS.Domain.Authorization;
using CMS.Domain.Common;
using Hangfire.Dashboard;

namespace CMS.Web.Jobs;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated users who are administrators
/// or hold the <c>System.ManageJobs</c> permission.
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true
            && (user.IsInRole(DomainDefaults.AdministratorRole)
                || user.HasClaim(AppClaimTypes.Permission, Permissions.System.ManageJobs));
    }
}
