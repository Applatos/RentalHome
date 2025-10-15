namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record AreaListItemDto(
    Guid Id,
    string Name,
    int HouseCount);
