namespace Sommerhus.Api.Dtos.Admin.Areas;

public record AreaListItemDto(Guid Id, string Slug, string Name, int HouseCount, int ImageCount);
