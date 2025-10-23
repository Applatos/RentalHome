using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Public.Houses;

public record HouseDetailsDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features);