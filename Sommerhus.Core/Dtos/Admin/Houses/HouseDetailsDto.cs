using Sommerhus.Core.Dtos.Admin.Features;
using Sommerhus.Core.Dtos.Admin.Pricing;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Admin.Houses;

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

