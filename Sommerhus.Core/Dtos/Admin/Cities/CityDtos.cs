using System.ComponentModel.DataAnnotations;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Admin.Cities;

public record CityListItemDto(Guid Id, string Name, string Zip, int HouseCount, int ImageCount);
public record CityDetailsDto(Guid Id, string Name, string Zip, string? Text, ImageDto[] Images);

public record CreateCityDto(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(10)] string Zip,
    [MaxLength(2000)] string? Text);

public record UpdateCityDto(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(10)] string Zip,
    [MaxLength(2000)] string? Text);

public class CityPageDto : PageResult<CityListItemDto> { }
