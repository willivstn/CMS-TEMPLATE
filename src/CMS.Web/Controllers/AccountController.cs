using CMS.Application.Identity;
using CMS.Shared.Results;
using CMS.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAppAuthenticationService = CMS.Application.Identity.IAuthenticationService;

namespace CMS.Web.Controllers;

/// <summary>
/// Handles interactive authentication. Actions stay thin: they bind input,
/// delegate to <see cref="IAppAuthenticationService"/>, and translate the
/// <see cref="Result"/> into a redirect or a re-rendered view.
/// </summary>
[AllowAnonymous]
public sealed class AccountController(IAppAuthenticationService auth) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await auth.LoginAsync(new LoginRequest(model.Email, model.Password, model.RememberMe), ct);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        switch (result.Value.Status)
        {
            case LoginStatus.Success:
                return RedirectToLocal(model.ReturnUrl);

            case LoginStatus.RequiresTwoFactor:
                return RedirectToAction(nameof(LoginWith2fa), new { model.RememberMe, model.ReturnUrl });

            case LoginStatus.LockedOut:
                ModelState.AddModelError(string.Empty, "This account is locked out. Try again later.");
                return View(model);

            case LoginStatus.NotAllowed:
                ModelState.AddModelError(string.Empty, "You must confirm your email before signing in.");
                return View(model);

            default:
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
        }
    }

    [HttpGet]
    public IActionResult LoginWith2fa(bool rememberMe, string? returnUrl = null)
        => View(new LoginWith2faViewModel { RememberMe = rememberMe, ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await auth.TwoFactorLoginAsync(
            new TwoFactorLoginRequest(model.TwoFactorCode, model.RememberMachine, model.RememberMe), ct);

        if (result.IsSuccess && result.Value.Status == LoginStatus.Success)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new RegisterRequest(model.Email, model.Password, model.ConfirmPassword, model.FirstName, model.LastName);
        var result = await auth.RegisterAsync(request, ConfirmationLink, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        return RedirectToAction(nameof(RegisterConfirmation));
    }

    [HttpGet]
    public IActionResult RegisterConfirmation() => View();

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token, CancellationToken ct)
    {
        var result = await auth.ConfirmEmailAsync(userId, token, ct);
        ViewBag.Succeeded = result.IsSuccess;
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await auth.ForgotPasswordAsync(model.Email,
            (_, token) => Url.Action(nameof(ResetPassword), "Account",
                new { email = model.Email, token }, Request.Scheme)!,
            ct);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email = null, string? token = null)
        => View(new ResetPasswordViewModel { Email = email ?? string.Empty, Token = token ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ResetPasswordRequest(model.Email, model.Token, model.Password, model.ConfirmPassword);
        var result = await auth.ResetPasswordAsync(request, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(ct);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ---- helpers ----

    private string ConfirmationLink(Guid userId, string token) =>
        Url.Action(nameof(ConfirmEmail), "Account", new { userId, token }, Request.Scheme)!;

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
}
