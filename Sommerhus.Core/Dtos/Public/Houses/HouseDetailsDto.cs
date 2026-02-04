using Sommerhus.Core.Dtos.Admin.Features;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Public.Houses;

public record HouseDetailsDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features);
