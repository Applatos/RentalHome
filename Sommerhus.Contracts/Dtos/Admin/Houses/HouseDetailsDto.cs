namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record HouseDetailsDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string? Address,
    Guid CityId,
    string? Description,
    string? Facilities,
    Guid? AreaId,
    Guid? CoverImageId);
