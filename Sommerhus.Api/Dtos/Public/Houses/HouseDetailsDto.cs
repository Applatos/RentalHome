using Sommerhus.Api.Dtos.Shared;

namespace Sommerhus.Api.Dtos.Public.Houses;

public record HouseDetailsDto(
    Guid Id,
    string Title,
    string? Subtitle,
    Guid CityId,
    string? Description,
    string? Facilities,
    string? CoverUrl,
    ImageDto[] Gallery,
    ImageDto? Floorplan,
    FeatureValueDto[] Features  // <- nu Shared-typen
);
