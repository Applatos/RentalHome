namespace Sommerhus.Contracts.Dtos.Shared;

public record FeatureValueDto(
    Guid Id,
    string Name,
    string Key,
    string ValueType,
    string? Unit,
    string? IconUrl,
    string Display);
