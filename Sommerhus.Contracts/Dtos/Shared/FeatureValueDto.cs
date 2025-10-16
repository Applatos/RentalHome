namespace Sommerhus.Contracts.Dtos.Admin.Features;

public sealed record FeatureValueDto(
    Guid Id,
    string Name,
    string ValueType,
    string? Unit,
    string? IconUrl,
    string RawValue
);