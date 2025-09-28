namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record PostFeatureValueDto(
    Guid FeatureId,
    string? RawValue);
