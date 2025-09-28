namespace Sommerhus.Contracts.Dtos.Public.Cities;

public record CityListItemDto(
    Guid Id,
    string Slug,
    string Name,
    string? Zip);
