namespace Sommerhus.Api.Dtos.Admin.Houses;

public record HouseListItemDto(Guid Id, string Title, string? City, string? Zip, string? Cover);
