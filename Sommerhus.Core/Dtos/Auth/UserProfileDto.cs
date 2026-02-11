using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Auth;

public sealed record UserProfileDto(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Address,
    string Role,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(500)]
    public string? Address { get; init; }

    [EmailAddress]
    public string? Email { get; init; }
}
