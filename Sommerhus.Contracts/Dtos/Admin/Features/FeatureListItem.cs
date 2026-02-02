using System;

namespace Sommerhus.Contracts.Dtos.Admin.Features;

// Consolidated with FeatureDetailsDto - use FeatureDetailsDto instead
// Keeping type alias for backward compatibility
public record FeatureListItem(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl);