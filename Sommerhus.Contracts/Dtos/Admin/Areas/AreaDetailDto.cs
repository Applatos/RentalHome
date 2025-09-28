using System.Collections.Generic;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record AreaDetailsDto(Guid Id, string Slug, string Name, string? Description, List<ImageDto> Images);
