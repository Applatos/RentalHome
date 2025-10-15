using System.Collections.Generic;
using Microsoft.VisualBasic;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record AreaDetailsDto(Guid Id, string Name, Guid? CityId, string? CityName, string? Description, IEnumerable<ImageDto> Images, IReadOnlyList<LookupItem> Cities = null);
