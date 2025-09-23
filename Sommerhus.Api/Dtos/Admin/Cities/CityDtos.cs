using Sommerhus.Api.Dtos.Shared;

namespace Sommerhus.Api.Dtos.Admin.Cities;

public record ZipDto(Guid Id, string Zip, string City);
public record CreateZipDto(string Zip, string City);

public class ZipPageDto : PageResult<ZipDto> { }

public record CityListItemDto(Guid Id, string Name, string Zip, string Slug, int HouseCount, int ImageCount);
public record CityDetailDto(Guid Id, string Name, string Zip, string Slug, string? Text, ImageDto[] Images);
public record CreateCityDto(string Name, string Zip, string Slug, string? Text);
public record UpdateCityDto(string Name, string Zip, string Slug, string? Text);

public class CityPageResult : PageResult<CityListItemDto> { }
