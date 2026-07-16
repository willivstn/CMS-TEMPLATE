using CMS.Application.Common.Abstractions;

namespace CMS.Infrastructure.Identity;

/// <summary>
/// Fallback <see cref="ICurrentUser"/> representing the system (no authenticated
/// principal). Used for background jobs, seeding, and design-time. The Web layer
/// registers an <c>HttpContext</c>-backed implementation that supersedes this for
/// request processing.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public string? UserName => "system";
    public bool IsAuthenticated => false;
    public string? IpAddress => null;
    public string? UserAgent => null;
    public bool HasPermission(string permission) => false;
}
