namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record UpsertAreaDto(
    string Name,
    IReadOnlyList<Guid>? CityIds,
    string? Description = null,
    IReadOnlyList<string>? Images = null);
