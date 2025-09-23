using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Dtos.Admin.Areas;

public record CreateAreaDto(
    [Required] string Name,
    string? Description,
    Guid? CityId,
    List<string>? Images
);
