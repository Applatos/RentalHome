using System.Collections.Generic;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Public.Areas;

public record AreaDetailDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<AreaHouseDto> Houses);

public record AreaHouseDto(Guid Id, string Title);
