using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Auth;

public sealed record RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }
}

public sealed record LoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthTokenResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
}
