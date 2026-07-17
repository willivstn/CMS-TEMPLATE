using CMS.Shared.Results;

namespace CMS.Application.Identity;

/// <summary>A freshly issued access token plus its rotating refresh token.</summary>
public sealed record ApiTokenResult(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Issues and rotates JWT access tokens and their refresh tokens for the REST API.
/// Implemented in the Web layer (JWT signing is web/config-bound); refresh tokens
/// are persisted hashed and rotated on use.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues a new access + refresh token pair for a user.</summary>
    Task<ApiTokenResult> IssueAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Validates and rotates a refresh token, returning a new pair.</summary>
    Task<Result<ApiTokenResult>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token so it can no longer be used.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
