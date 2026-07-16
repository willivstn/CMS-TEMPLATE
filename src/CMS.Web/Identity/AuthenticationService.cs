using System.Text;
using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Common;
using CMS.Domain.Entities.Identity;
using CMS.Domain.Entities.System;
using CMS.Domain.Enums;
using CMS.Shared.Results;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using IAppAuthenticationService = CMS.Application.Identity.IAuthenticationService;

namespace CMS.Web.Identity;

/// <summary>
/// Implements the authentication flows over ASP.NET Core Identity. Lives in the
/// Web layer because sign-in is inherently tied to <c>HttpContext</c> (the auth
/// cookie). Keeps controllers thin: they validate nothing beyond binding and
/// translate <see cref="Result"/> outcomes to views.
/// </summary>
public sealed class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IApplicationDbContext db,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<ResetPasswordRequest> resetValidator,
    ILogger<AuthenticationService> logger) : IAppAuthenticationService
{
    public async Task<Result> RegisterAsync(
        RegisterRequest request, CallbackLinkFactory confirmationLinkFactory, CancellationToken cancellationToken = default)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(ToError(validation));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result.Failure(ToError(created));
        }

        await userManager.AddToRoleAsync(user, DomainDefaults.ViewerRole);

        if (userManager.Options.SignIn.RequireConfirmedAccount)
        {
            await SendConfirmationEmailAsync(user, confirmationLinkFactory);
        }

        logger.LogInformation("New account registered: {Email}", request.Email);
        return Result.Success();
    }

    public async Task<Result<LoginOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<LoginOutcome>(ToError(validation));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            await RecordLoginAsync(null, request.Email, succeeded: false, "InvalidCredentials", cancellationToken);
            return Result.Success(new LoginOutcome(LoginStatus.Failed));
        }

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, request.RememberMe, lockoutOnFailure: true);

        var status = MapSignIn(result);
        await RecordLoginAsync(user.Id, request.Email, result.Succeeded, status.ToString(), cancellationToken);

        if (result.Succeeded)
        {
            user.LastLoginUtc = clock.UtcNow;
            await userManager.UpdateAsync(user);
            await WriteAuditAsync(user.Id, request.Email, AuditAction.Login, cancellationToken);
        }

        return Result.Success(new LoginOutcome(status));
    }

    public async Task<Result<LoginOutcome>> TwoFactorLoginAsync(
        TwoFactorLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return Result.Success(new LoginOutcome(LoginStatus.Failed));
        }

        var code = request.Code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            code, request.RememberMe, request.RememberMachine);

        var status = MapSignIn(result);
        await RecordLoginAsync(user.Id, user.Email ?? user.UserName ?? string.Empty, result.Succeeded, $"2FA:{status}", cancellationToken);

        if (result.Succeeded)
        {
            await WriteAuditAsync(user.Id, user.Email, AuditAction.Login, cancellationToken);
        }

        return Result.Success(new LoginOutcome(status));
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var userName = currentUser.UserName;
        await signInManager.SignOutAsync();

        if (userId is { } id)
        {
            await WriteAuditAsync(id, userName, AuditAction.Logout, cancellationToken);
        }
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Auth.UserNotFound", "Account not found."));
        }

        var decoded = DecodeToken(token);
        var result = await userManager.ConfirmEmailAsync(user, decoded);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    public async Task<Result> ResendConfirmationAsync(
        string email, CallbackLinkFactory confirmationLinkFactory, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
        {
            await SendConfirmationEmailAsync(user, confirmationLinkFactory);
        }

        // Do not reveal whether the account exists.
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(
        string email, CallbackLinkFactory resetLinkFactory, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && (await userManager.IsEmailConfirmedAsync(user)
            || !userManager.Options.SignIn.RequireConfirmedAccount))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = resetLinkFactory(user.Id, EncodeToken(token));
            await emailSender.SendAsync(email, "Reset your password",
                $"<p>Reset your password by <a href=\"{link}\">clicking here</a>.</p>", cancellationToken);
        }

        // Do not reveal whether the account exists.
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await resetValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(ToError(validation));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Avoid enumeration: behave as if the token were invalid.
            return Result.Failure(Error.Validation("Auth.InvalidToken", "Invalid or expired reset token."));
        }

        var result = await userManager.ResetPasswordAsync(user, DecodeToken(request.Token), request.Password);
        return result.Succeeded ? Result.Success() : Result.Failure(ToError(result));
    }

    // ---- helpers ----

    private async Task SendConfirmationEmailAsync(ApplicationUser user, CallbackLinkFactory linkFactory)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = linkFactory(user.Id, EncodeToken(token));
        await emailSender.SendAsync(user.Email!, "Confirm your email",
            $"<p>Confirm your account by <a href=\"{link}\">clicking here</a>.</p>");
    }

    private async Task RecordLoginAsync(Guid? userId, string userName, bool succeeded, string? reason, CancellationToken ct)
    {
        db.LoginHistories.Add(new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Succeeded = succeeded,
            FailureReason = succeeded ? null : reason,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            TimestampUtc = clock.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }

    private async Task WriteAuditAsync(Guid userId, string? userName, AuditAction action, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = DomainDefaults.DefaultTenantId,
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityName = nameof(ApplicationUser),
            EntityId = userId.ToString(),
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            TimestampUtc = clock.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }

    private static LoginStatus MapSignIn(SignInResult result) => result switch
    {
        { Succeeded: true } => LoginStatus.Success,
        { RequiresTwoFactor: true } => LoginStatus.RequiresTwoFactor,
        { IsLockedOut: true } => LoginStatus.LockedOut,
        { IsNotAllowed: true } => LoginStatus.NotAllowed,
        _ => LoginStatus.Failed,
    };

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string DecodeToken(string token) =>
        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

    private static Error ToError(FluentValidation.Results.ValidationResult validation) =>
        Error.Validation("Auth.Validation", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

    private static Error ToError(IdentityResult result) =>
        Error.Validation("Auth.Identity", string.Join(" ", result.Errors.Select(e => e.Description)));
}
