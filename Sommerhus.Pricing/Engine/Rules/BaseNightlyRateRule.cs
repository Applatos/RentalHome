// Base nightly price fra RateSeason
using Sommerhus.Pricing.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class BaseNightlyRateRule : IPriceRule
{
    private readonly IRatePlanStore _store;
    public BaseNightlyRateRule(IRatePlanStore store) => _store = store;

    public async Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        var nights = ctx.Nights.ToList();

        if (nights.Count == 0) return;

        var plan = ctx.RatePlan ??= await _store.GetActivePlanAsync(ctx.Request.HouseId, ct);

        ctx.Currency = plan.Currency;

        // Hent sæsoner der overlapper
        var from = nights.First();
        var to = nights.Last();

        var seasons = plan.Seasons
            .Where(s => (s.StartDate <= to) && (s.EndDate >= from))
            .ToList();

        foreach (var date in nights)
        {
            var season = seasons.FirstOrDefault(x => x.StartDate <= date && date <= x.EndDate);
            ctx.NightlyRates[date] = season!.NightlyPrice;
            ctx.Items.Add(new PriceLineItem("BASE", $"Nat {date} ({season.Name})", season.NightlyPrice));
        }
    }
}

