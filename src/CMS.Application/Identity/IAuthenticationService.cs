using CMS.Shared.Results;

namespace CMS.Application.Identity;

/// <summary>
/// Encapsulates authentication flows over ASP.NET Core Identity so controllers
/// stay thin. Returns <see cref="Result"/> values (never throws for expected
/// failures) and records login history. URL composition is delegated to the Web
/// layer via <see cref="CallbackLinkFactory"/>.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>Creates a local account and, when required, emails a confirmation link.</summary>
    Task<Result> RegisterAsync(
        RegisterRequest request,
        CallbackLinkFactory confirmationLinkFactory,
        CancellationToken cancellationToken = default);

    /// <summary>Attempts an interactive sign-in and records the attempt.</summary>
    Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Completes sign-in using a two-factor authenticator code.</summary>
    Task<Result<LoginOutcome>> TwoFactorLoginAsync(TwoFactorLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Signs the current user out.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Confirms an email address using the emailed token.</summary>
    Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);

    /// <summary>Sends (or resends) an email-confirmation link if the account exists and is unconfirmed.</summary>
    Task<Result> ResendConfirmationAsync(string email, CallbackLinkFactory confirmationLinkFactory, CancellationToken cancellationToken = default);

    /// <summary>Emails a password-reset link. Always succeeds outwardly to avoid account enumeration.</summary>
    Task<Result> ForgotPasswordAsync(string email, CallbackLinkFactory resetLinkFactory, CancellationToken cancellationToken = default);

    /// <summary>Resets a password using the emailed token.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
