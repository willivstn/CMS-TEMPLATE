namespace CMS.Application.Features.Roles;

/// <summary>A row in the roles list.</summary>
public sealed record RoleListItemDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    int UserCount,
    int PermissionCount);

/// <summary>The editable shape of a role.</summary>
public sealed record RoleEditDto(Guid Id, string Name, string? Description, bool IsSystem);

/// <summary>A role plus the permission names currently granted to it.</summary>
public sealed record RolePermissionsDto(Guid Id, string Name, bool IsSystem, IReadOnlyList<string> SelectedPermissions);
