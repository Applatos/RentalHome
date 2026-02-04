namespace Sommerhus.Core.Dtos.Shared;

public record PostFeatureValueDto(
    Guid FeatureId,
    string? RawValue);
