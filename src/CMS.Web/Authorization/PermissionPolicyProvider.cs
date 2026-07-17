using CMS.Web.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CMS.Web.Authorization;

/// <summary>
/// Fabricates authorization policies for permission names on demand. A policy
/// named "perm:Content.Edit" yields a policy with a single
/// <see cref="PermissionRequirement"/>, avoiding the need to register hundreds of
/// named policies at startup. Non-permission policies fall through to the default
/// provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var permission = HasPermissionAttribute.GetPermission(policyName);
        if (permission is not null)
        {
            // Accept the MVC cookie, API JWT and API-key schemes so the same
            // [HasPermission] attribute guards both the admin UI and the REST API.
            var policy = new AuthorizationPolicyBuilder(
                    IdentityConstants.ApplicationScheme,
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
