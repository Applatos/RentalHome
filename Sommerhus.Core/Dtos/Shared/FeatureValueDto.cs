namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Represents a feature value assigned to a house.
/// </summary>
public sealed record FeatureValueDto(
    Guid Id,
    string Name,
    string ValueType,
    string? Unit,
    string? IconUrl,
    string RawValue
);
