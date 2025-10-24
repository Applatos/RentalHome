using Sommerhus.Pricing.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions;

public interface IRatePlanStore
{
    Task<RatePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct);
}