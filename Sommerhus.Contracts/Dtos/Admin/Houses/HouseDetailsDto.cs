using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;
// === READ-DTO til Admin (bruges i Edit GET) ===

public record HouseDetailsDto(
    Guid Id,
    string Name,
    Guid CityId,
    string CityLabel,
    Guid? AreaId,
    string? AreaLabel,
    string Address,
    string Description,
    DateTime CreatedUtc,
    IReadOnlyList<FeatureValueDto>? Features,
    IReadOnlyList<ImageDto>? Images);

