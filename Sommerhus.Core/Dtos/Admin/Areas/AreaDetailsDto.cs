using System.Collections.Generic;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Admin.Areas;

public sealed record AreaDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<Guid> CityIds,
    IReadOnlyList<LookupItem> Cities,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<AreaHouseDto>? Houses = null);

public sealed record AreaHouseDto(Guid Id, string Title);
