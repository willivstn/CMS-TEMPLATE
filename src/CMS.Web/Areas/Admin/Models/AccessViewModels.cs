using System.ComponentModel.DataAnnotations;
using CMS.Application.Features.Permissions;
using CMS.Application.Features.Users;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>Create-user form.</summary>
public sealed class UserCreateViewModel
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "First name"), MaxLength(128)]
    public string? FirstName { get; set; }

    [Display(Name = "Last name"), MaxLength(128)]
    public string? LastName { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<string> SelectedRoles { get; set; } = [];

    public IReadOnlyList<RoleOptionDto> AllRoles { get; set; } = [];
}

/// <summary>Edit-user form (email is read-only here).</summary>
public sealed class UserEditViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    [Display(Name = "First name"), MaxLength(128)]
    public string? FirstName { get; set; }

    [Display(Name = "Last name"), MaxLength(128)]
    public string? LastName { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public List<string> SelectedRoles { get; set; } = [];

    public IReadOnlyList<RoleOptionDto> AllRoles { get; set; } = [];
}

/// <summary>Admin password-reset form.</summary>
public sealed class ResetUserPasswordViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Create/edit-role form.</summary>
public sealed class RoleFormViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}

/// <summary>The role permission matrix.</summary>
public sealed class RolePermissionsViewModel
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public IReadOnlyList<PermissionGroupDto> Groups { get; set; } = [];
    public HashSet<string> Selected { get; set; } = [];
}

/// <summary>The per-user permission override matrix (tri-state: inherit/grant/deny).</summary>
public sealed class UserPermissionsViewModel
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<PermissionGroupDto> Groups { get; set; } = [];
    public HashSet<string> Grants { get; set; } = [];
    public HashSet<string> Denies { get; set; } = [];
}
