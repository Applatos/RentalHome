namespace Sommerhus.Mvc.ViewModels.Admin.Houses;

/// <summary>How well a house's effective season calendar covers the coming months.</summary>
public enum HouseCalendarCoverage
{
    /// <summary>Every night in the checked period has a season.</summary>
    Covered,

    /// <summary>No house calendar and no group calendar.</summary>
    NoCalendar,

    /// <summary>A calendar with no season spans.</summary>
    NoSpans,

    /// <summary>A run of nights without a season, followed by more spans.</summary>
    Gap,

    /// <summary>The last span ends before the checked period does.</summary>
    EndsSoon
}

/// <summary>
/// What stops a house from getting a price in the next <see cref="CoverageMonths"/> months: a
/// missing or short season calendar, no active price plan, or seasons in the calendar without a
/// price. A night without a season price cannot be quoted or booked.
/// </summary>
public sealed record HousePricingCheck(
    HouseCalendarCoverage Coverage,
    DateOnly? GapFrom,
    DateOnly? GapTo,
    DateOnly? CalendarEnd,
    bool HasActivePlan,
    IReadOnlyList<string> UnpricedCodes)
{
    public const int CoverageMonths = 12;

    public bool CalendarNeedsAttention => Coverage != HouseCalendarCoverage.Covered;

    public static HousePricingCheck For(AdminHouseDetailsDto house, DateOnly today)
    {
        var horizon = today.AddMonths(CoverageMonths);
        var spans = (house.Calendar ?? [])
            .Where(s => s.EndDate >= s.StartDate)
            .OrderBy(s => s.StartDate)
            .ToList();

        var plan = house.Pricing;
        var hasActivePlan = plan is { IsActive: true };
        var unpriced = hasActivePlan ? FindUnpricedCodes(spans, plan!, today, horizon) : [];

        var hasCalendar = house.CalendarOverrideId.HasValue || !string.IsNullOrEmpty(house.CalendarSource);
        if (!hasCalendar)
        {
            return new(HouseCalendarCoverage.NoCalendar, null, null, null, hasActivePlan, unpriced);
        }

        if (spans.Count == 0)
        {
            return new(HouseCalendarCoverage.NoSpans, null, null, null, hasActivePlan, unpriced);
        }

        var calendarEnd = spans.Max(s => s.EndDate);

        // Walk the spans in start order; the cursor is the first night not yet known to be covered.
        var cursor = today;
        foreach (var span in spans.Where(s => s.EndDate >= today))
        {
            if (span.StartDate > cursor || cursor >= horizon)
            {
                break;
            }

            if (span.EndDate >= cursor)
            {
                cursor = span.EndDate.AddDays(1);
            }
        }

        if (cursor >= horizon)
        {
            return new(HouseCalendarCoverage.Covered, null, null, calendarEnd, hasActivePlan, unpriced);
        }

        var nextStart = spans
            .Where(s => s.StartDate > cursor)
            .Select(s => (DateOnly?)s.StartDate)
            .FirstOrDefault();

        return nextStart is { } next
            ? new(HouseCalendarCoverage.Gap, cursor, next.AddDays(-1), calendarEnd, hasActivePlan, unpriced)
            : new(HouseCalendarCoverage.EndsSoon, null, null, calendarEnd, hasActivePlan, unpriced);
    }

    // Season codes used in the checked period that the plan gives no positive price, in first-use order.
    private static IReadOnlyList<string> FindUnpricedCodes(
        IEnumerable<SeasonSpanDto> spans,
        PricePlanDetailsDto plan,
        DateOnly today,
        DateOnly horizon)
    {
        var priced = plan.SeasonPrices
            .Where(p => p.NightlyPrice > 0)
            .Select(p => p.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return spans
            .Where(s => s.EndDate >= today && s.StartDate < horizon && !priced.Contains(s.Code))
            .Select(s => s.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
