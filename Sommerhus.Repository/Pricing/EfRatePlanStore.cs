using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Domain.Models.Pricing;
using Sommerhus.Repository;

namespace Sommerhus.Repository.Pricing;

public sealed class EfRatePlanStore : IRatePlanStore
{
    private readonly AppDbContext _db;

    public EfRatePlanStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PricePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct)
    {
        return await _db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .Where(p => p.HouseId == houseId && p.IsActive)
            .OrderByDescending(p => p.UpdatedUtc ?? p.CreatedUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SeasonSpan>> GetSeasonCalendarAsync(Guid houseId, CancellationToken ct)
    {
        var houseGroup = await _db.Houses
            .AsNoTracking()
            .Where(h => h.Id == houseId)
            .Select(h => new { h.Id, h.GroupId })
            .FirstOrDefaultAsync(ct);

        if (houseGroup?.GroupId is not Guid groupId)
        {
            return Array.Empty<SeasonSpan>();
        }

        return await _db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToListAsync(ct);
    }
}
