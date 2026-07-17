using Asp.Versioning;
using CMS.Contracts;
using CMS.Domain.Entities.Identity;
using ITokenService = CMS.Application.Identity.ITokenService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CMS.Web.Api.V1;

/// <summary>
/// Token endpoints for the REST API: password sign-in issues a JWT access token
/// plus a rotating refresh token; refresh exchanges a valid refresh token for a
/// new pair; revoke invalidates one. Sign-in is rate-limited.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(UserManager<ApplicationUser> userManager, ITokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !user.EmailConfirmed
            || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ApiError("auth.invalid_credentials", "Invalid credentials."));
        }

        var result = await tokens.IssueAsync(user.Id, Ip(), ct);
        return Ok(new TokenResponse(result.AccessToken, result.RefreshToken, result.ExpiresAtUtc));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await tokens.RefreshAsync(request.RefreshToken, Ip(), ct);
        return result.IsSuccess
            ? Ok(new TokenResponse(result.Value.AccessToken, result.Value.RefreshToken, result.Value.ExpiresAtUtc))
            : Unauthorized(new ApiError(result.Error.Code, result.Error.Message));
    }

    [HttpPost("revoke")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await tokens.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
