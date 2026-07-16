using Microsoft.AspNetCore.Authorization;

namespace CMS.Web.Authorization;

/// <summary>An authorization requirement satisfied by holding a named permission.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    /// <summary>The permission the caller must hold, e.g. "Content.Edit".</summary>
    public string Permission { get; } = permission;
}
