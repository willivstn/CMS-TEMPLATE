using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Authorization;
using CMS.Domain.Entities.Identity;
using CMS.Shared.Results;
using CMS.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CMS.Web.Identity;

/// <summary>
/// Issues JWT access tokens (carrying role and permission claims) and rotating,
/// hashed refresh tokens for the REST API. Refresh rotation revokes the used token
/// and chains it to its replacement for auditability.
/// </summary>
public sealed class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    IPermissionService permissions,
    IApplicationDbContext db,
    IDateTimeProvider clock,
    IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<ApiTokenResult> IssueAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var expiresAt = clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = await BuildAccessTokenAsync(user, expiresAt);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, cancellationToken);

        return new ApiTokenResult(accessToken, refreshToken, expiresAt);
    }

    public async Task<Result<ApiTokenResult>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return Result.Failure<ApiTokenResult>(Error.Unauthorized("Token.Invalid", "Invalid or expired refresh token."));
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<ApiTokenResult>(Error.Unauthorized("Token.Invalid", "Account is not active."));
        }

        // Rotate: revoke the used token and issue a fresh pair.
        var newRefresh = await CreateRefreshTokenAsync(user.Id, ipAddress, cancellationToken);
        existing.RevokedOnUtc = clock.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReplacedByTokenHash = Hash(newRefresh);

        var expiresAt = clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = await BuildAccessTokenAsync(user, expiresAt);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(new ApiTokenResult(accessToken, newRefresh, expiresAt));
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is { RevokedOnUtc: null })
        {
            existing.RevokedOnUtc = clock.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<string> BuildAccessTokenAsync(ApplicationUser user, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in await permissions.GetPermissionsAsync(user.Id))
        {
            claims.Add(new Claim(AppClaimTypes.Permission, permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(raw),
            ExpiresOnUtc = clock.UtcNow.AddDays(_options.RefreshTokenDays),
            CreatedOnUtc = clock.UtcNow,
            CreatedByIp = ipAddress,
        });

        await db.SaveChangesAsync(cancellationToken);
        return raw;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
