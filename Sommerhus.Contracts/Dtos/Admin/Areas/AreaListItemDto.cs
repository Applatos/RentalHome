namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record AreaListItemDto(
    Guid Id,
    string Slug,
    string Name,
    int HouseCount);
