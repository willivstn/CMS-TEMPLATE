using System.Security.Claims;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Authorization;
using CMS.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Identity;

/// <summary>
/// Enriches the authenticated principal at sign-in with profile claims and one
/// claim per effective permission. Because permissions become claims, per-request
/// authorization is a fast in-memory check with no database round-trip. When
/// grants change, bumping the user's security stamp forces the claims to refresh.
/// </summary>
public sealed class AppUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options,
    IPermissionService permissionService)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));
        }

        identity.AddClaim(new Claim("display_name", user.DisplayName));

        var permissions = await permissionService.GetPermissionsAsync(user.Id);
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        return identity;
    }
}
