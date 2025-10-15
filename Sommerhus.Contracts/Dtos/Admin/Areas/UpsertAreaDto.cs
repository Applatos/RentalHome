namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record UpsertAreaDto(
    string Name,
    Guid? CityId,
    string? Description = null,
    IReadOnlyList<string>? Images = null);
