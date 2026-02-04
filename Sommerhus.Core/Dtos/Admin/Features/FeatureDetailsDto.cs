namespace Sommerhus.Core.Dtos.Admin.Features;

public record FeatureDetailsDto(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl);
