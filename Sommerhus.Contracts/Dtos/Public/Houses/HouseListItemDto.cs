namespace Sommerhus.Contracts.Dtos.Public.Houses;

public record HouseListItemDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string? City,
    string? Zip,
    string? CoverUrl);
