using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Identity;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }
}
