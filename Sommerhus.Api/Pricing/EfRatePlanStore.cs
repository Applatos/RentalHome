using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Pricing.Abstractions;
using Sommerhus.Pricing.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sommerhus.Api.Pricing;

public sealed class EfRatePlanStore : IRatePlanStore
{
    private readonly AppDbContext _db;

    public EfRatePlanStore(AppDbContext db) => _db = db;

    public async Task<RatePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct)
    {
        return await _db.RatePlans
            .AsNoTracking()
            .Include(p => p.Seasons)
            .Include(p => p.Modifiers)
            .Where(p => p.HouseId == houseId && p.IsActive)
            .OrderByDescending(p => p.UpdatedUtc ?? p.CreatedUtc)
            .FirstOrDefaultAsync(ct);
    }
}