namespace Sommerhus.Core.Dtos.Public.Cities;

public record CityListItemDto(
    Guid Id,
    string Name,
    string? Zip);
