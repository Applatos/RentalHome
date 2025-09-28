using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record HouseAdminDetailsDto(
    Guid Id,
    string Title,
    CityMiniDto? City,
    string? Address,
    DateTime CreatedUtc,
    IReadOnlyList<ImageDto> Images);

public record CityMiniDto(
    string Name,
    string? Zip);
