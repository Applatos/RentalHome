namespace Sommerhus.Core.Dtos.Admin.Areas;

public record AreaListItemDto(
    Guid Id,
    string Name,
    int HouseCount);
