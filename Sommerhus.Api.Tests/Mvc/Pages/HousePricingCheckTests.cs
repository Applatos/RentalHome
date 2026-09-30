using FluentAssertions;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.ViewModels.Admin.Houses;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class HousePricingCheckTests
{
    private static readonly DateOnly Today = new(2026, 9, 30);

    private static SeasonSpanDto Span(string code, DateOnly start, DateOnly end) => new(Guid.NewGuid(), start, end, code);

    private static PricePlanDetailsDto Plan(bool active, params (string Code, decimal Price)[] prices)
    {
        var planId = Guid.NewGuid();
        return new PricePlanDetailsDto(planId, Guid.NewGuid(), "Standard", "DKK", active, DateTime.UtcNow, null,
            prices.Select(p => new SeasonPriceDto(Guid.NewGuid(), planId, p.Code, p.Price)).ToList());
    }

    private static AdminHouseDetailsDto House(
        IReadOnlyList<SeasonSpanDto>? calendar,
        string? calendarSource = "Group: Vesterhavet",
        Guid? overrideId = null,
        PricePlanDetailsDto? plan = null)
        => new(Guid.NewGuid(), "Blåvand Strand 4", null, null, null, null, [], [], Guid.NewGuid(), null, [], [],
            DateTime.UtcNow, calendar, plan, Guid.NewGuid(), EntityStatus.Published,
            CalendarOverrideId: overrideId, CalendarSource: calendarSource);

    [Fact]
    public void For_NoGroupCalendarAndNoOverride_IsNoCalendar()
        => HousePricingCheck.For(House([], calendarSource: null), Today).Coverage.Should().Be(HouseCalendarCoverage.NoCalendar);

    [Fact]
    public void For_OverrideWithoutSpans_IsNoSpans()
        => HousePricingCheck.For(House([], calendarSource: null, overrideId: Guid.NewGuid()), Today)
            .Coverage.Should().Be(HouseCalendarCoverage.NoSpans);

    [Fact]
    public void For_SpansCoveringTheNextTwelveMonths_IsCovered()
    {
        var check = HousePricingCheck.For(House([
            Span("B", new DateOnly(2026, 9, 1), new DateOnly(2027, 5, 31)),
            Span("A", new DateOnly(2027, 6, 1), new DateOnly(2027, 12, 31))]), Today);

        check.Coverage.Should().Be(HouseCalendarCoverage.Covered);
        check.CalendarNeedsAttention.Should().BeFalse();
    }

    [Fact]
    public void For_CalendarEndingWithinTwelveMonths_IsEndsSoonWithTheEndDate()
    {
        var check = HousePricingCheck.For(House([Span("A", new DateOnly(2026, 1, 1), new DateOnly(2027, 3, 31))]), Today);

        check.Coverage.Should().Be(HouseCalendarCoverage.EndsSoon);
        check.CalendarEnd.Should().Be(new DateOnly(2027, 3, 31));
    }

    [Fact]
    public void For_CalendarEndingOnTheLastNightOfTheYear_IsCovered()
        => HousePricingCheck.For(House([Span("A", new DateOnly(2026, 1, 1), Today.AddMonths(12).AddDays(-1))]), Today)
            .Coverage.Should().Be(HouseCalendarCoverage.Covered);

    [Fact]
    public void For_CalendarThatHasEnded_IsEndsSoonWithThePastEndDate()
    {
        var check = HousePricingCheck.For(House([Span("A", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))]), Today);

        check.Coverage.Should().Be(HouseCalendarCoverage.EndsSoon);
        check.CalendarEnd.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public void For_NightsWithoutASeasonBetweenSpans_IsGapWithItsFirstAndLastNight()
    {
        var check = HousePricingCheck.For(House([
            Span("B", new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)),
            Span("A", new DateOnly(2026, 11, 15), new DateOnly(2028, 1, 1))]), Today);

        check.Coverage.Should().Be(HouseCalendarCoverage.Gap);
        check.GapFrom.Should().Be(new DateOnly(2026, 11, 1));
        check.GapTo.Should().Be(new DateOnly(2026, 11, 14));
    }

    [Fact]
    public void For_OverlappingSpans_CountAsCovered()
        => HousePricingCheck.For(House([
                Span("B", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 31)),
                Span("A", new DateOnly(2026, 12, 1), new DateOnly(2027, 12, 31))]), Today)
            .Coverage.Should().Be(HouseCalendarCoverage.Covered);

    [Fact]
    public void For_ActivePlan_ListsSeasonsInUseWithoutAPositivePrice()
    {
        var calendar = new[]
        {
            Span("C", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            Span("B", new DateOnly(2026, 9, 1), new DateOnly(2027, 5, 31)),
            Span("A", new DateOnly(2027, 6, 1), new DateOnly(2027, 8, 31)),
            Span("D", new DateOnly(2027, 9, 1), new DateOnly(2027, 12, 31))
        };

        var check = HousePricingCheck.For(House(calendar, plan: Plan(true, ("A", 1200m), ("B", 0m))), Today);

        check.HasActivePlan.Should().BeTrue();
        check.UnpricedCodes.Should().Equal("B", "D");
    }

    [Fact]
    public void For_InactiveOrMissingPlan_HasNoActivePlan()
    {
        var calendar = new[] { Span("A", new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31)) };

        HousePricingCheck.For(House(calendar, plan: Plan(false, ("A", 1200m))), Today).HasActivePlan.Should().BeFalse();
        HousePricingCheck.For(House(calendar, plan: null), Today).HasActivePlan.Should().BeFalse();
    }
}
