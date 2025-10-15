namespace Sommerhus.Contracts.Dtos.Public.Cities;

public record CityListItemDto(
    Guid Id,
    string Name,
    string? Zip);
