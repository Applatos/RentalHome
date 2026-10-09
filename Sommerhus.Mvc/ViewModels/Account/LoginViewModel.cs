using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.ViewModels.Account;

/// <remarks>
/// Labels and messages are resource keys; MVC's data annotation localization looks them up in
/// <see cref="SharedResource"/>, so they follow the request culture.
/// </remarks>
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Account.Validation.Required")]
    [Display(Name = "Account.Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account.Validation.Required")]
    [DataType(DataType.Password)]
    [Display(Name = "Account.Password")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
