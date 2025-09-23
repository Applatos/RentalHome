namespace Sommerhus.Api.Dtos.Shared;

public record FeatureValueDto(
    Guid FeatureId,
    string Name,
    string Key,
    string ValueType,
    string? Unit,
    string? IconUrl,
    string Display);

public record PostFeatureValueDto(Guid FeatureId, string RawValue);
