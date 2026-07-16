using Microsoft.AspNetCore.Identity;

namespace CMS.Domain.Entities.Identity;

/// <summary>Join between users and roles, with navigation properties for querying.</summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public ApplicationUser User { get; set; } = null!;
    public ApplicationRole Role { get; set; } = null!;
}

/// <summary>A claim belonging to a user.</summary>
public class ApplicationUserClaim : IdentityUserClaim<Guid>
{
    public ApplicationUser User { get; set; } = null!;
}

/// <summary>An external login (Google, Microsoft, …) linked to a user.</summary>
public class ApplicationUserLogin : IdentityUserLogin<Guid>
{
    public ApplicationUser User { get; set; } = null!;
}

/// <summary>A persisted token (e.g. authenticator key) for a user.</summary>
public class ApplicationUserToken : IdentityUserToken<Guid>
{
    public ApplicationUser User { get; set; } = null!;
}

/// <summary>A claim belonging to a role.</summary>
public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
    public ApplicationRole Role { get; set; } = null!;
}
