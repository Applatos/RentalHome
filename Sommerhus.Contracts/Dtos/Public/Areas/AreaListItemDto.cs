namespace Sommerhus.Contracts.Dtos.Public.Areas;

public record AreaListItemDto(
    Guid Id,
    string Slug,
    string Name,
    int HouseCount);
