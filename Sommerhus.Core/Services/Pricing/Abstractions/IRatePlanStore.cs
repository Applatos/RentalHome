using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public interface IRatePlanStore
{
    Task<PricePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct);
    Task<IReadOnlyList<SeasonSpan>> GetSeasonCalendarAsync(Guid houseId, CancellationToken ct);

    /// <summary>
    /// Season display names keyed by season code (case-insensitive).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetSeasonNamesAsync(CancellationToken ct);
}
