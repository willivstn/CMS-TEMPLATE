namespace CMS.Application.Identity;

/// <summary>Data required to register a new local account.</summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName = null,
    string? LastName = null);

/// <summary>Credentials for an interactive sign-in.</summary>
public sealed record LoginRequest(string Email, string Password, bool RememberMe);

/// <summary>A two-factor authenticator code submitted during the 2FA step of sign-in.</summary>
public sealed record TwoFactorLoginRequest(string Code, bool RememberMachine, bool RememberMe);

/// <summary>A password-reset submission carrying the emailed token.</summary>
public sealed record ResetPasswordRequest(string Email, string Token, string Password, string ConfirmPassword);

/// <summary>The outcome status of an interactive sign-in attempt.</summary>
public enum LoginStatus
{
    Success = 0,
    RequiresTwoFactor = 1,
    LockedOut = 2,
    NotAllowed = 3,
    Failed = 4,
}

/// <summary>The result of a sign-in attempt.</summary>
public sealed record LoginOutcome(LoginStatus Status);

/// <summary>Builds an absolute callback URL for the given user id and token.</summary>
/// <remarks>
/// Supplied by the Web layer (which owns routing) so the service can compose and
/// send confirmation/reset emails without depending on ASP.NET routing.
/// </remarks>
public delegate string CallbackLinkFactory(Guid userId, string token);
