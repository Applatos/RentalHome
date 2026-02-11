using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? ReturnUrl { get; set; }
}
