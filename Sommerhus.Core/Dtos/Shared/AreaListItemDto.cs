namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Area list item used by both Admin and Public contexts.
/// </summary>
public record AreaListItemDto(
    Guid Id,
    string Name,
    int HouseCount);
