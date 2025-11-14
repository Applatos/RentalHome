using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(5, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;
}
