using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public record UpdateHouseDto(
    [param: Required, StringLength(140)] string Title,
    string? Subtitle,
    string? Address,
    [param: Required] Guid CityId,
    string? Description,
    string? Facilities
);

