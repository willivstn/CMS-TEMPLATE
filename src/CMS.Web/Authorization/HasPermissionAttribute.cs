using Microsoft.AspNetCore.Authorization;

namespace CMS.Web.Authorization;

/// <summary>
/// Guards an action or controller with a permission requirement, e.g.
/// <c>[HasPermission(Permissions.Content.Edit)]</c>. The policy is fabricated on
/// demand by <see cref="PermissionPolicyProvider"/>, so no permission needs to be
/// pre-registered as a named policy.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Prefix distinguishing permission policies from ordinary named policies.</summary>
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permission) => Policy = PolicyPrefix + permission;

    /// <summary>Extracts the permission name from a policy name, or null if not a permission policy.</summary>
    public static string? GetPermission(string policyName) =>
        policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal)
            ? policyName[PolicyPrefix.Length..]
            : null;
}
