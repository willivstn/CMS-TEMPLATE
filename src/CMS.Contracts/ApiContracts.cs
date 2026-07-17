namespace CMS.Contracts;

// Public request/response models for the versioned REST API. Kept separate from
// internal view models so the API surface can evolve independently.

/// <summary>Credentials for API sign-in.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>A refresh-token exchange request.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>The token pair returned by sign-in / refresh.</summary>
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc, string TokenType = "Bearer");

/// <summary>A content item as exposed by the public API.</summary>
public sealed record ContentResponse(
    Guid Id,
    string Type,
    string Title,
    string Slug,
    string? Summary,
    string? Body,
    string? Category,
    string? Author,
    IReadOnlyList<string> Tags,
    DateTimeOffset? PublishedOnUtc);

/// <summary>A standard error payload for API responses.</summary>
public sealed record ApiError(string Code, string Message);
