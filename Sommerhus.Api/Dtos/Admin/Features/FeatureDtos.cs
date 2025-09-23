namespace Sommerhus.Api.Dtos.Admin.Features;

public record FeatureDto(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);
public record UpsertFeatureDto(string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);
public record FeatureValueDto(Guid FeatureId, string Name, string Key, string ValueType, string? Unit, string? IconUrl, string Display);
public record PostFeatureValueDto(Guid FeatureId, string RawValue);
