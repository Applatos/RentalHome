using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Cities;

public record ZipListItemDto(Guid Id, string Zip, string City);
public record CreateZipDto(string Zip, string City);

public class ZipPageDto : PageResult<ZipListItemDto>{ }






public record CityListItemDto(Guid Id, string Name, string Zip, string Slug, int HouseCount, int ImageCount);
public record CityDetailsDto(Guid Id, string Name, string Zip, string Slug, string? Text, ImageDto[] Images);
public record CreateCityDto(string Name, string Zip, string Slug, string? Text);
public record UpdateCityDto(string Name, string Zip, string Slug, string? Text);

public class CityPageDto : PageResult<CityListItemDto> { }
