using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Pricing.Engine.Rules;

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
            return;
        }

        ctx.Currency = plan.Currency;

        var calendar = await _store.GetSeasonCalendarAsync(ctx.Request.HouseId, ct);
        if (calendar.Count == 0 || plan.SeasonPrices.Count == 0)
        {
            return;
        }

        var rateLookup = plan.SeasonPrices
            .ToDictionary(r => r.Code, r => r, StringComparer.OrdinalIgnoreCase);

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
            if (segment is null)
            {
                continue;
            }

            if (!rateLookup.TryGetValue(segment.Code, out var rate))
            {
                continue;
            }

            ctx.NightlyRates[date] = rate.NightlyPrice;

            if (nightsBySeason.TryGetValue(segment.Code, out var existing))
            {
                nightsBySeason[segment.Code] = (existing.Count + 1, rate.NightlyPrice);
            }
            else
            {
                nightsBySeason[segment.Code] = (1, rate.NightlyPrice);
            }
        }

        // Add summarized line items instead of per-night breakdown
        foreach (var (code, (count, rate)) in nightsBySeason.OrderBy(x => x.Key))
        {
            var total = count * rate;
            ctx.Items.Add(new PriceQuoteLineItemDto("BASE", $"{count} nætter ({code})", total));
        }
    }
}
