using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Pricing.Engine.Rules;

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
            ctx.Items.Add(new PriceQuoteLineItemDto("BASE", $"Nat {date} ({segment.Code})", rate.NightlyPrice));
        }
    }
}
