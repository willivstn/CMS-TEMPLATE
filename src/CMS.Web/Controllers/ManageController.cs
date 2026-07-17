using System.Text;
using System.Text.Encodings.Web;
using CMS.Domain.Entities.Identity;
using CMS.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

/// <summary>
/// Self-service account security: enable/disable authenticator-app two-factor
/// authentication and view recovery codes. Any authenticated user manages their
/// own account.
/// </summary>
[Authorize]
public sealed class ManageController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UrlEncoder urlEncoder) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Security()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        ViewBag.TwoFactorEnabled = user.TwoFactorEnabled;
        ViewBag.HasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user) is not null;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EnableAuthenticator()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var model = await BuildEnableModelAsync(user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEnableModelAsync(user));
        }

        var code = model.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var valid = await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
        {
            ModelState.AddModelError(nameof(model.Code), "Verification code is invalid.");
            return View(await BuildEnableModelAsync(user));
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        TempData["flash:success"] = "Two-factor authentication enabled.";
        model.RecoveryCodes = recoveryCodes?.ToArray() ?? [];
        return View("RecoveryCodes", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2fa()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await signInManager.RefreshSignInAsync(user);

        TempData["flash:success"] = "Two-factor authentication disabled.";
        return RedirectToAction(nameof(Security));
    }

    private async Task<EnableAuthenticatorViewModel> BuildEnableModelAsync(ApplicationUser user)
    {
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = await userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
        var issuer = urlEncoder.Encode("CMS Template");
        var account = urlEncoder.Encode(email);
        return new EnableAuthenticatorViewModel
        {
            SharedKey = FormatKey(key!),
            AuthenticatorUri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6",
        };
    }

    private static string FormatKey(string key)
    {
        var result = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
        {
            result.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        }

        return result.ToString().Trim().ToLowerInvariant();
    }
}
