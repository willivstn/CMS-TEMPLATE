namespace CMS.Application.Features.Permissions;

/// <summary>A single permission in the catalogue.</summary>
public sealed record PermissionListItemDto(string Name, string DisplayName, string? Description);

/// <summary>Permissions grouped by their logical group, for grids and matrices.</summary>
public sealed record PermissionGroupDto(string Group, IReadOnlyList<PermissionListItemDto> Items);
