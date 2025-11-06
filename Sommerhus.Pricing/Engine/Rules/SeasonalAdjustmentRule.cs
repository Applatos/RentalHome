using Sommerhus.Pricing.Abstractions;
using Sommerhus.Pricing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Engine.Rules;

    // Just a marker class for now
public class SeasonalAdjustmentRule : IPriceRule
{
    IRatePlanStore _store;
    public SeasonalAdjustmentRule(IRatePlanStore store) => _store = store;

    public async Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        var nights = ctx.Nights.ToList();
        if (nights.Count == 0) return;

        var plan = ctx.RatePlan ??= await _store.GetActivePlanAsync(ctx.Request.HouseId, ct);
        //if (plan is null || plan.Modifiers.Count == 0) return;

        var baseTotal = nights.Sum(n => ctx.NightlyRates.TryGetValue(n, out var rate) ? rate : 0m);
        var weekendNights = nights.Where(IsWeekendNight).ToList();

        //foreach (var modifier in plan.Modifiers.Where(m => m.IsActive))
        //{
        //    if (modifier.Scope == PriceScope.PerNight)
        //    {
        //        var applicableNights = FilterNightsForModifier(modifier, nights, weekendNights);
        //        if (applicableNights.Count == 0) continue;

        //        var amount = CalculatePerNightAmount(modifier, applicableNights, ctx);
        //        if (amount == 0) continue;

        //        ctx.Items.Add(new PriceLineItem("MOD", modifier.Name, amount));
        //    }
        //    else
        //    {
        //        if (!ShouldApplyBookingModifier(modifier, nights.Count, weekendNights.Count)) continue;

        //        var amount = CalculatePerBookingAmount(modifier, baseTotal);
        //        if (amount == 0) continue;

        //        ctx.Items.Add(new PriceLineItem("MOD", modifier.Name, amount));
        //    }
        //}
    }

    private static IReadOnlyList<DateOnly> FilterNightsForModifier(PriceModifier modifier, IReadOnlyList<DateOnly> nights, IReadOnlyList<DateOnly> weekendNights)
    {
        return modifier.Trigger switch
        {
            ModifierTrigger.Always => nights,
            ModifierTrigger.Weekend => weekendNights,
            ModifierTrigger.MinNights => modifier.ThresholdNights.HasValue && nights.Count >= modifier.ThresholdNights
                ? nights
                : Array.Empty<DateOnly>(),
            _ => Array.Empty<DateOnly>()
        };
    }

    private static bool ShouldApplyBookingModifier(PriceModifier modifier, int nightsCount, int weekendNightCount)
    {
        return modifier.Trigger switch
        {
            ModifierTrigger.Always => true,
            ModifierTrigger.Weekend => weekendNightCount > 0,
            ModifierTrigger.MinNights => modifier.ThresholdNights.HasValue && nightsCount >= modifier.ThresholdNights,
            _ => false
        };
    }

    private static decimal CalculatePerNightAmount(PriceModifier modifier, IReadOnlyList<DateOnly> nights, PricingContext ctx)
    {
        if (nights.Count == 0) return 0;

        return modifier.Kind switch
        {
            AdjustmentKind.Absolute => modifier.Value * nights.Count,
            AdjustmentKind.Percent => Math.Round(nights.Sum(n => ctx.NightlyRates.TryGetValue(n, out var rate) ? rate : 0) * modifier.Value, 2),
            _ => 0
        };
    }

    private static decimal CalculatePerBookingAmount(PriceModifier modifier, decimal baseTotal)
    {
        return modifier.Kind switch
        {
            AdjustmentKind.Absolute => modifier.Value,
            AdjustmentKind.Percent => Math.Round(baseTotal * modifier.Value, 2),
            _ => 0
        };
    }

    private static bool IsWeekendNight(DateOnly date)
        => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
}
