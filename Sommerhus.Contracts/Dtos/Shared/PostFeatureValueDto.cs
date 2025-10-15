namespace Sommerhus.Contracts.Dtos.Shared;

public record PostFeatureValueDto(
    Guid FeatureId,
    string? RawValue);
