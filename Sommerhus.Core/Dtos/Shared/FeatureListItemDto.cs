namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Feature list item for display purposes.
/// </summary>
public sealed record FeatureListItemDto(
    Guid Id,
    string Name,
    string Key,
    string ValueType,
    string? Unit = null,
    string? IconUrl = null);
