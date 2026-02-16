using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Public.Pricing;

public sealed class PriceSummaryService(AppDbContext db) : IPriceSummaryService
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

    public async Task RecomputeSummaryAsync(Guid houseId, CancellationToken ct)
    {
        var plan = await db.PricePlans
            .AsNoTracking()
            .Where(p => p.HouseId == houseId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.Currency,
                Prices = p.SeasonPrices.Select(sp => sp.NightlyPrice)
            })
            .FirstOrDefaultAsync(ct);

        var existing = await db.Set<HousePriceSummary>().FirstOrDefaultAsync(s => s.HouseId == houseId, ct);

        if (plan is null || !plan.Prices.Any())
        {
            if (existing is not null)
            {
                db.Set<HousePriceSummary>().Remove(existing);
                await db.SaveChangesAsync(ct);
            }

            await db.Set<PriceQuote>()
                .Where(q => q.HouseId == houseId)
                .ExecuteDeleteAsync(ct);

            return;
        }

        var min = plan.Prices.Min();
        var max = plan.Prices.Max();

        if (existing is null)
        {
            existing = new HousePriceSummary
            {
                HouseId = houseId
            };

            db.Set<HousePriceSummary>().Add(existing);
        }

        existing.MinNightlyPrice = min;
        existing.MaxNightlyPrice = max;
        existing.Currency = plan.Currency;
        existing.ComputedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await db.Set<PriceQuote>()
            .Where(q => q.HouseId == houseId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task RecomputeSummariesForGroupAsync(Guid groupId, CancellationToken ct)
    {
        var houseIds = await db.Houses
            .AsNoTracking()
            .Where(h => h.GroupId == groupId)
            .Select(h => h.Id)
            .ToListAsync(ct);

        foreach (var houseId in houseIds)
        {
            await RecomputeSummaryAsync(houseId, ct);
        }
    }
}
