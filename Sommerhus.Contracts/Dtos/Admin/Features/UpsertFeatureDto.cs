namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record UpsertFeatureDto(
    string Name,
    string Key,
    string ValueType,
    string? Unit,
    string? IconUrl,
    int SortOrder);
