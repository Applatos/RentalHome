using Sommerhus.Api.Dtos.Shared;

namespace Sommerhus.Api.Dtos.Admin.Cities;

public record ZipDto(Guid Id, string Zip, string City);
public record CreateZipDto(string Zip, string City);

public class ZipPageDto : PageResult<ZipDto> { }

public record CityListItemDto(string City, string Zip, int Count);
