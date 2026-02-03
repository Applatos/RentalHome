using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record HouseDetailsDto(
    Guid Id,
    string Name,
    Guid CityId,
    string CityLabel,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<LookupItem> Areas,
    string Address,
    string Description,
    DateTime CreatedUtc,
    IReadOnlyList<FeatureValueDto>? Features,
    IReadOnlyList<ImageDto>? Images,
    IReadOnlyList<SeasonSpanDto> Calendar,
    PricePlanDetailsDto? Pricing,
    Guid? GroupId);

