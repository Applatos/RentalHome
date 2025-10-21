using System.Collections.Generic;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public sealed record AreaDetailsDto(
    Guid Id,
    string Name,
    Guid? CityId,
    string? CityName,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<LookupItem>? Cities = null,
    IReadOnlyList<AreaHouseDto>? Houses = null);

public sealed record AreaHouseDto(Guid Id, string Title);