namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record HouseRowDto(
    Guid Id,
    string Title,
    string? City,
    DateTime CreatedUtc);
