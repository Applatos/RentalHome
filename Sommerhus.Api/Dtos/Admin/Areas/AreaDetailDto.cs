using System.Collections.Generic;

namespace Sommerhus.Api.Dtos.Admin.Areas;

public record AreaDetailDto(Guid Id, string Name, string? Description, List<AreaImageItemDto> Images);

public record AreaImageItemDto(Guid Id, string Url);
