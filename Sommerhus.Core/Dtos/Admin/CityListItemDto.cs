using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Admin;

public record CityListItemDto(Guid Id, string Name, string Zip, int HouseCount, int ImageCount);
