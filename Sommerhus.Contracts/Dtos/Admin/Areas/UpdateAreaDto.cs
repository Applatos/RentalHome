namespace Sommerhus.Contracts.Dtos.Admin.Areas;

public record UpdateAreaDto(
    string Name,
    Guid? CityId,
    string? Description = null,
    IReadOnlyList<string>? Images = null);
