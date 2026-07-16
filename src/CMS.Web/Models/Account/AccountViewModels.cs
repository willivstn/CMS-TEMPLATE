using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Models.Account;

/// <summary>Sign-in form. DataAnnotations drive client-side validation; the
/// authentication service performs authoritative server-side validation.</summary>
public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "First name"), MaxLength(128)]
    public string? FirstName { get; set; }

    [Display(Name = "Last name"), MaxLength(128)]
    public string? LastName { get; set; }

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class LoginWith2faViewModel
{
    [Required]
    [StringLength(8, MinimumLength = 6)]
    [Display(Name = "Authenticator code")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Remember this device")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ResendConfirmationViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
