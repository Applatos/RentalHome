using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Application.Pricing.Abstractions;

public interface IRatePlanStore
{
    Task<PricePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct);
    Task<IReadOnlyList<SeasonSpan>> GetSeasonCalendarAsync(Guid houseId, CancellationToken ct);
}
