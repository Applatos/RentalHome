using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Represents a feature value assigned to a house. <see cref="Key"/> identifies the feature
/// across languages ("max_guests"), so a page can pick out specific facts.
/// </summary>
public sealed record FeatureValueDto(
    Guid Id,
    string Name,
    FeatureValueType ValueType,
    string? Unit,
    string? IconUrl,
    string RawValue,
    string? Key = null,
    FeatureCategory? Category = null
);
