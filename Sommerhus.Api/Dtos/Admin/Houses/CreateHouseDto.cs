using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Dtos.Admin.Houses;

public record CreateHouseDto(
    [param: Required, StringLength(140)] string Title,
    string? Subtitle,
    string? Address,
    [param: Required] Guid CityId,
    string? Description,
    string? Facilities
);

