namespace CMS.Application.Features.Users;

/// <summary>A row in the users list.</summary>
public sealed record UserListItemDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    bool IsLockedOut,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? LastLoginUtc);

/// <summary>Full user detail for the details page.</summary>
public sealed record UserDetailsDto(
    Guid Id,
    string DisplayName,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    int AccessFailedCount,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? LastLoginUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<LoginHistoryDto> RecentLogins);

/// <summary>A recent sign-in attempt shown on the user details page.</summary>
public sealed record LoginHistoryDto(
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    DateTimeOffset TimestampUtc);

/// <summary>The editable shape of a user (create/edit forms).</summary>
public sealed record UserEditDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IReadOnlyList<string> SelectedRoles);

/// <summary>A selectable role option.</summary>
public sealed record RoleOptionDto(string Name, string? Description);
