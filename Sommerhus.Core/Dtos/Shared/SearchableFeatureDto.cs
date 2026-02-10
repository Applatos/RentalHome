using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Lightweight DTO for search UI filter generation — grouped by category.
/// </summary>
public sealed record SearchableFeatureDto(
    string Key,
    string Name,
    FeatureValueType ValueType,
    string? Unit = null,
    string? Options = null);
