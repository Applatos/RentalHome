using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Account;

/// <remarks>
/// Labels and messages are resource keys; MVC's data annotation localization looks them up in
/// <see cref="SharedResource"/>, so they follow the request culture.
/// </remarks>
public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Account.Validation.Required")]
    [MinLength(3, ErrorMessage = "Account.Validation.MinLength")]
    [MaxLength(50, ErrorMessage = "Account.Validation.MaxLength")]
    [Display(Name = "Account.Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account.Validation.Required")]
    [EmailAddress(ErrorMessage = "Account.Validation.Email")]
    [Display(Name = "Account.Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account.Validation.Required")]
    [MinLength(8, ErrorMessage = "Account.Validation.MinLength")]
    [DataType(DataType.Password)]
    [Display(Name = "Account.Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account.Validation.Required")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Account.Validation.PasswordsDoNotMatch")]
    [Display(Name = "Account.ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Account.Validation.MaxLength")]
    [Display(Name = "Account.FirstName")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "Account.Validation.MaxLength")]
    [Display(Name = "Account.LastName")]
    public string? LastName { get; set; }

    [MaxLength(20, ErrorMessage = "Account.Validation.MaxLength")]
    [Display(Name = "Account.Phone")]
    public string? Phone { get; set; }

    public string? ReturnUrl { get; set; }
}
