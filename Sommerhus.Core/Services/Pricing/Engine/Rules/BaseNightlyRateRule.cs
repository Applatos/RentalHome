using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Pricing.Engine.Rules;

/// <summary>
/// Prices each night from the season span covering it in the house's effective calendar and the
/// active plan's price for that span's season code. A night with no plan, no calendar, no covering
/// span or no price for the span's code is recorded in <see cref="PricingContext.UnpricedNights"/>.
/// </summary>
public sealed class BaseNightlyRateRule : IPriceRule
{
    private readonly IRatePlanStore _store;

    public BaseNightlyRateRule(IRatePlanStore store)
    {
        _store = store;
    }

    public async Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        var nights = ctx.Nights.ToList();

        if (nights.Count == 0)
        {
            return;
        }

        var plan = ctx.RatePlan ??= await _store.GetActivePlanAsync(ctx.Request.HouseId, ct);
        if (plan is null)
        {
            ctx.UnpricedNights.AddRange(nights);
            return;
        }

        ctx.Currency = plan.Currency;

        var calendar = await _store.GetSeasonCalendarAsync(ctx.Request.HouseId, ct);

        // A price of zero or less counts as missing: a night is never free.
        var rateLookup = plan.SeasonPrices
            .Where(r => r.NightlyPrice > 0)
            .GroupBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().NightlyPrice, StringComparer.OrdinalIgnoreCase);

        var from = nights.First();
        var to = nights.Last();

        var relevantSegments = calendar
            .Where(s => s.StartDate <= to && s.EndDate >= from)
            .ToList();

        // Group nights by season code for cleaner breakdown
        var nightsBySeason = new Dictionary<string, (int Count, decimal Rate)>(StringComparer.OrdinalIgnoreCase);

        foreach (var date in nights)
        {
            var segment = relevantSegments.FirstOrDefault(x => x.StartDate <= date && date <= x.EndDate);
            if (segment is null || !rateLookup.TryGetValue(segment.Code, out var rate))
            {
                ctx.UnpricedNights.Add(date);
                continue;
            }

            ctx.NightlyRates[date] = rate;

            nightsBySeason[segment.Code] = nightsBySeason.TryGetValue(segment.Code, out var existing)
                ? (existing.Count + 1, rate)
                : (1, rate);
        }

        if (nightsBySeason.Count == 0)
        {
            return;
        }

        var seasonNames = await _store.GetSeasonNamesAsync(ct);

        // Add summarized line items instead of per-night breakdown
        foreach (var (code, (count, rate)) in nightsBySeason.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var name = seasonNames.TryGetValue(code, out var seasonName) ? seasonName : code;
            var unit = count == 1 ? "night" : "nights";

            ctx.Items.Add(new PriceQuoteLineItemDto(
                "BASE",
                $"{count} {unit} ({name})",
                count * rate,
                Nights: count,
                UnitPrice: rate,
                SeasonCode: code,
                SeasonName: name));
        }
    }
}
