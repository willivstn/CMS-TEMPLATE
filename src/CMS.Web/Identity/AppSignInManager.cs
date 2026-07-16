using CMS.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CMS.Web.Identity;

/// <summary>
/// Extends the default <see cref="SignInManager{TUser}"/> to also honour the
/// custom <see cref="ApplicationUser.IsActive"/> flag: deactivated accounts cannot
/// sign in even if they are otherwise valid and unlocked.
/// </summary>
public sealed class AppSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            return false;
        }

        return await base.CanSignInAsync(user);
    }
}
