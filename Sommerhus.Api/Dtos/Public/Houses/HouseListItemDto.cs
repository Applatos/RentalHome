namespace Sommerhus.Api.Dtos.Public.Houses;

public record HouseListItemDto(Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? Cover);
