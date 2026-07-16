namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Ambient information about the user making the current request. Implemented in
/// the Web layer over <c>HttpContext</c> and consumed by services, interceptors
/// (for audit stamping) and authorization logic.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or null when anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The authenticated user's name/email, or null when anonymous.</summary>
    string? UserName { get; }

    bool IsAuthenticated { get; }

    /// <summary>The caller's IP address, when available.</summary>
    string? IpAddress { get; }

    /// <summary>The caller's user-agent string, when available.</summary>
    string? UserAgent { get; }

    /// <summary>Returns true if the current user holds the given permission.</summary>
    bool HasPermission(string permission);
}
