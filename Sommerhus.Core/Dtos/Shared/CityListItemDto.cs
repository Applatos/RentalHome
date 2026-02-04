namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// City list item for display purposes.
/// </summary>
public sealed record CityListItemDto(
    Guid Id,
    string Name,
    string? Zip);
