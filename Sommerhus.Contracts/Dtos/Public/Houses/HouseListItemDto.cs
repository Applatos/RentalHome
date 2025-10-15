namespace Sommerhus.Contracts.Dtos.Public.Houses;

public record HouseListItemDto(
    Guid Id,
    string Title,
    string CityName,
    string Zip,
    string? CoverUrl);