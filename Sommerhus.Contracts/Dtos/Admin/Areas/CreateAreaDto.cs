namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record CreateAreaDto(
    string Name,
    Guid? CityId,
    string? Description = null,
    IReadOnlyList<string>? Images = null);
