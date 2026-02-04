using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Admin;

public record CityDetailsDto(Guid Id, string Name, string Zip, string? Text, ImageDto[] Images);
