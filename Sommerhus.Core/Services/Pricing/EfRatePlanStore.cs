using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Domain.Models.Pricing;
using Sommerhus.Core;

namespace Sommerhus.Core.Services.Pricing;

public sealed class EfRatePlanStore : IRatePlanStore
{
    private readonly AppDbContext db;

    public EfRatePlanStore(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<PricePlan?> GetActivePlanAsync(Guid houseId, CancellationToken ct)
    {
        return await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .Where(p => p.HouseId == houseId && p.IsActive)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SeasonSpan>> GetSeasonCalendarAsync(Guid houseId, CancellationToken ct)
    {
        var house = await db.Houses
            .AsNoTracking()
            .Include(h => h.Group)
            .Where(h => h.Id == houseId)
            .Select(h => new { h.CalendarOverrideId, GroupCalendarId = h.Group != null ? h.Group.DefaultCalendarId : null })
            .FirstOrDefaultAsync(ct);

        var calendarId = house?.CalendarOverrideId ?? house?.GroupCalendarId;
        if (calendarId is null)
            return Array.Empty<SeasonSpan>();

        return await db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.CalendarId == calendarId.Value)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToListAsync(ct);
    }
}
