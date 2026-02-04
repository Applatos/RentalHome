namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Feature details with optional admin-specific fields.
/// </summary>
public sealed record FeatureDetailsDto(
    Guid Id,
    string Name,
    string Key,
    string ValueType,
    string? Unit = null,
    string? IconUrl = null,
    // Admin-specific fields could be added here if needed
    string? Description = null);
