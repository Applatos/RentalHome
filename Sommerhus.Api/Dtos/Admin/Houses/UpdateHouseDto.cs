using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Dtos.Admin.Houses;

public record UpdateHouseDto(
    [property: Required, StringLength(140)] string Title,
    string? Subtitle,
    string? Address,
    [property: Required] Guid CityId,
    string? Description,
    string? Facilities
);
