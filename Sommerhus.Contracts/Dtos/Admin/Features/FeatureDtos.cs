namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record FeatureDto(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);