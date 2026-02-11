namespace Sommerhus.Core.Dtos.Admin;

public sealed record UserListItemDto(
    string Id,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string Role,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record AssignOwnerRequest
{
    public string? OwnerId { get; init; }
}

public sealed record ChangeUserRoleRequest
{
    public string Role { get; init; } = string.Empty;
}
