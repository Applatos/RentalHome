using Microsoft.EntityFrameworkCore;
using Sommerhus.Repository;
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

    public async Task<PricePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct)
    {
        return await _db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .Where(p => p.HouseId == houseId && p.IsActive)
            .OrderByDescending(p => p.UpdatedUtc ?? p.CreatedUtc)
            .FirstOrDefaultAsync(ct);
    }


    public async Task<IReadOnlyList<SeasonSpan>> GetSeasonCalendarAsync(Guid GroupId, CancellationToken ct)
    {
        return await _db.SeasonSpans
            .AsNoTracking()
            .Include(s => s.GroupId)
            .Where(s => s.GroupId == GroupId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToListAsync(ct);
    }
}
