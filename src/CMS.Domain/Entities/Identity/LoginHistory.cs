using CMS.Domain.Common;

namespace CMS.Domain.Entities.Identity;

/// <summary>
/// A record of a sign-in attempt, used for the per-user login-history screen and
/// security monitoring. Both successful and failed attempts are captured.
/// </summary>
public class LoginHistory : Entity<Guid>
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>The user name/email that was submitted (kept even on failure).</summary>
    public string UserName { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    /// <summary>Reason for a failed attempt (e.g. "LockedOut", "InvalidPassword").</summary>
    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
