using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Public.Pricing;

public sealed class PriceSummaryService(AppDbContext db, ISearchIndexer searchIndexer) : IPriceSummaryService
{
    public async Task<Dictionary<Guid, (decimal? Min, decimal? Max, string? Currency)>> GetSummariesAsync(
        IEnumerable<Guid> houseIds,
        CancellationToken ct)
    {
        var ids = houseIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var summaries = await db.Set<HousePriceSummary>()
            .AsNoTracking()
            .Where(s => ids.Contains(s.HouseId))
            .ToListAsync(ct);

        return summaries.ToDictionary(
            s => s.HouseId,
            s => (s.MinNightlyPrice, s.MaxNightlyPrice, (string?)s.Currency));
    }

    public async Task RecomputeSummariesAsync(IEnumerable<Guid> houseIds, CancellationToken ct)
    {
        var ids = houseIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        await db.Set<PriceQuote>()
            .Where(q => ids.Contains(q.HouseId))
            .ExecuteDeleteAsync(ct);

        foreach (var houseId in ids)
        {
            var fromPrice = await StoreFromPriceAsync(houseId, ct);
            await RefreshSearchPricesAsync(houseId, fromPrice, ct);
        }
    }

    public Task RecomputeSummaryAsync(Guid houseId, CancellationToken ct)
        => RecomputeSummariesAsync([houseId], ct);

    public async Task RecomputeSummariesForCalendarAsync(Guid calendarId, CancellationToken ct)
    {
        var houseIds = await db.Houses
            .AsNoTracking()
            .Where(h => h.CalendarOverrideId == calendarId
                        || (h.CalendarOverrideId == null && h.Group != null && h.Group.DefaultCalendarId == calendarId))
            .Select(h => h.Id)
            .ToListAsync(ct);

        await RecomputeSummariesAsync(houseIds, ct);
    }

    private async Task<(decimal Min, decimal Max, string Currency)?> StoreFromPriceAsync(Guid houseId, CancellationToken ct)
    {
        var fromPrice = await ComputeFromPriceAsync(houseId, ct);
        var existing = await db.Set<HousePriceSummary>().FirstOrDefaultAsync(s => s.HouseId == houseId, ct);

        if (fromPrice is null)
        {
            if (existing is not null)
            {
                db.Set<HousePriceSummary>().Remove(existing);
                await db.SaveChangesAsync(ct);
            }

            return null;
        }

        if (existing is null)
        {
            existing = new HousePriceSummary
            {
                HouseId = houseId
            };

            db.Set<HousePriceSummary>().Add(existing);
        }

        existing.MinNightlyPrice = fromPrice.Value.Min;
        existing.MaxNightlyPrice = fromPrice.Value.Max;
        existing.Currency = fromPrice.Value.Currency;
        existing.ComputedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return fromPrice;
    }

    // Only the price columns of the search document change. A full reindex would also restamp its
    // UpdatedAtUtc, which orders the public list, so every price refresh would reshuffle it.
    private async Task RefreshSearchPricesAsync(Guid houseId, (decimal Min, decimal Max, string Currency)? fromPrice, CancellationToken ct)
    {
        decimal? min = fromPrice?.Min;
        decimal? max = fromPrice?.Max;
        var currency = fromPrice?.Currency;

        var updated = await db.HouseSearchDocuments
            .Where(d => d.HouseId == houseId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(d => d.MinNightlyPrice, min)
                .SetProperty(d => d.MaxNightlyPrice, max)
                .SetProperty(d => d.Currency, currency), ct);

        if (updated == 0)
        {
            await searchIndexer.UpdateHouseAsync(houseId, ct);
        }
    }

    /// <summary>
    /// The lowest and highest nightly price of the house's active plan, counting only season codes
    /// that its effective calendar uses on spans ending today or later. Null when nothing qualifies,
    /// so a house that cannot be quoted shows no from-price.
    /// </summary>
    private async Task<(decimal Min, decimal Max, string Currency)?> ComputeFromPriceAsync(Guid houseId, CancellationToken ct)
    {
        // Same plan choice as EfRatePlanStore.GetActivePlanAsync, so the from-price matches quotes.
        var plan = await db.PricePlans
            .AsNoTracking()
            .Where(p => p.HouseId == houseId && p.IsActive)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Select(p => new
            {
                p.Currency,
                Prices = p.SeasonPrices.Select(sp => new { sp.Code, sp.NightlyPrice }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (plan is null)
        {
            return null;
        }

        var calendarId = await db.Houses
            .AsNoTracking()
            .Where(h => h.Id == houseId)
            .Select(h => h.CalendarOverrideId ?? (h.Group != null ? h.Group.DefaultCalendarId : null))
            .FirstOrDefaultAsync(ct);

        if (calendarId is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcomingCodes = await db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.CalendarId == calendarId.Value && s.EndDate >= today)
            .Select(s => s.Code)
            .Distinct()
            .ToListAsync(ct);

        var codeSet = new HashSet<string>(upcomingCodes, StringComparer.OrdinalIgnoreCase);
        var prices = plan.Prices
            .Where(p => p.NightlyPrice > 0 && codeSet.Contains(p.Code))
            .Select(p => p.NightlyPrice)
            .ToList();

        return prices.Count == 0
            ? null
            : (prices.Min(), prices.Max(), plan.Currency);
    }
}
