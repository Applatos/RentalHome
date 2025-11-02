using System;
using System.Collections.Generic;

namespace Sommerhus.Api.Services.Admin.Houses;

public sealed record FeatureUpsertOutcome(bool HouseFound, IReadOnlyList<Guid> MissingFeatureIds)
{
    public static FeatureUpsertOutcome NotFound { get; } = new(false, Array.Empty<Guid>());
    public static FeatureUpsertOutcome Success { get; } = new(true, Array.Empty<Guid>());

    public bool HasMissingFeatures => MissingFeatureIds.Count > 0;
}