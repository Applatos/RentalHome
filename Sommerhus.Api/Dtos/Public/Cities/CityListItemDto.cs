namespace Sommerhus.Api.Dtos.Public.Cities;

public record CityListItemDto(Guid Id, string Slug, string City, string Zip, int Count);
