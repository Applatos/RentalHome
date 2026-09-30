using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Public;

public sealed class QuoteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly PricingScenario scenario;

    public QuoteTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task Quote_SevenHighSeasonNights_ReturnsExactTotalWithIncludedVat()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var arrival = new DateOnly(Year, 7, 6);

        var quote = await scenario.QuoteAsync(houseId, arrival, arrival.AddDays(7), guests: 4);

        // 7 nights * 1000 + 2 extra guests * 50 * 7 nights + cleaning 950
        const decimal expectedTotal = 7 * HighRate + 2 * GuestFeePerNight * 7 + CleaningFee;
        quote.Nights.Should().Be(7);
        quote.Subtotal.Should().Be(expectedTotal);
        quote.Tax.Should().Be(0m);
        quote.Total.Should().Be(expectedTotal);
        quote.VatIncluded.Should().Be(expectedTotal * 0.2m);

        quote.Items.Should().HaveCount(3);
        var baseLine = quote.Items.Single(i => i.Code == "BASE");
        baseLine.Amount.Should().Be(7 * HighRate);
        baseLine.Nights.Should().Be(7);
        baseLine.UnitPrice.Should().Be(HighRate);
        baseLine.SeasonCode.Should().Be("A");
        baseLine.SeasonName.Should().Be("Højsæson");
        baseLine.Text.Should().Be("7 nights (Højsæson)");

        var guestLine = quote.Items.Single(i => i.Code == "GUEST");
        guestLine.Amount.Should().Be(2 * GuestFeePerNight * 7);
        guestLine.Nights.Should().Be(7);
        guestLine.Guests.Should().Be(2);
        guestLine.UnitPrice.Should().Be(GuestFeePerNight);

        var cleaningLine = quote.Items.Single(i => i.Code == "CLEAN");
        cleaningLine.Amount.Should().Be(CleaningFee);
        cleaningLine.Nights.Should().BeNull();
        cleaningLine.UnitPrice.Should().BeNull();
    }

    [Fact]
    public async Task Quote_StayAcrossTwoSeasons_PricesEachNightByItsSeason()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();

        // Jun 28, 29, 30 are low season; Jul 1 and 2 are high season.
        var quote = await scenario.QuoteAsync(houseId, new DateOnly(Year, 6, 28), new DateOnly(Year, 7, 3), guests: 2);

        quote.Items.Where(i => i.Code == "BASE").Should().BeEquivalentTo(new[]
        {
            new { SeasonCode = "A", Nights = (int?)2, UnitPrice = (decimal?)HighRate, Amount = 2 * HighRate, SeasonName = "Højsæson" },
            new { SeasonCode = "B", Nights = (int?)3, UnitPrice = (decimal?)LowRate, Amount = 3 * LowRate, SeasonName = "Lavsæson" }
        });
        quote.Items.Should().NotContain(i => i.Code == "GUEST");
        quote.Total.Should().Be(2 * HighRate + 3 * LowRate + CleaningFee);
        quote.VatIncluded.Should().Be(quote.Total * 0.2m);
    }

    [Fact]
    public async Task Quote_RepeatedFromCache_ReturnsSameLinesAndVat()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var arrival = new DateOnly(Year, 7, 10);

        var first = await scenario.QuoteAsync(houseId, arrival, arrival.AddDays(3), guests: 3);
        var second = await scenario.QuoteAsync(houseId, arrival, arrival.AddDays(3), guests: 3);

        (await scenario.CountCachedQuotesAsync(houseId)).Should().Be(1);
        second.Should().BeEquivalentTo(first);
        second.VatIncluded.Should().Be(second.Total * 0.2m);
    }

    [Fact]
    public async Task Quote_HouseWithoutGroup_ReturnsUnpricedNightsAndIsNotCached()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        var arrival = new DateOnly(Year, 7, 6);

        using var res = await scenario.GetQuoteAsync(houseId, arrival, arrival.AddDays(7), guests: 2);

        var errors = await ReadValidationErrorsAsync(res, PricingErrors.UnpricedNights);
        errors.Should().ContainSingle().Which.Should().Contain("7 nights").And.Contain($"{arrival:yyyy-MM-dd}");
        (await scenario.CountCachedQuotesAsync(houseId)).Should().Be(0);
    }

    [Fact]
    public async Task Quote_NightInCalendarGap_ReturnsUnpricedNightsUntilTheGapIsFilled()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", new DateOnly(Year, 7, 1), new DateOnly(Year, 7, 10));
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        // Jul 9 and 10 are priced, Jul 11 and 12 are not.
        var arrival = new DateOnly(Year, 7, 9);
        var departure = new DateOnly(Year, 7, 13);

        using (var res = await scenario.GetQuoteAsync(houseId, arrival, departure, guests: 2))
        {
            var errors = await ReadValidationErrorsAsync(res, PricingErrors.UnpricedNights);
            errors.Single().Should().Contain("2 nights").And.Contain($"{Year}-07-11");
        }

        await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 7, 11), new DateOnly(Year, 7, 31));

        var quote = await scenario.QuoteAsync(houseId, arrival, departure, guests: 2);
        quote.Total.Should().Be(2 * HighRate + 2 * LowRate + CleaningFee);
    }

    [Fact]
    public async Task Quote_SeasonWithoutPlanPrice_ReturnsUnpricedNights()
    {
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate });

        using var res = await scenario.GetQuoteAsync(houseId, new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 4), guests: 2);

        (await ReadValidationErrorsAsync(res, PricingErrors.UnpricedNights)).Single().Should().Contain("3 nights");
    }

    [Fact]
    public async Task Quote_InactivePlan_ReturnsUnpricedNights()
    {
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate }, isActive: false);

        using var res = await scenario.GetQuoteAsync(houseId, new DateOnly(Year, 7, 6), new DateOnly(Year, 7, 8), guests: 2);

        await ReadValidationErrorsAsync(res, PricingErrors.UnpricedNights);
    }

    [Fact]
    public async Task Quote_MoreGuestsThanCapacity_ReturnsGuestsError()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();

        using var res = await scenario.GetQuoteAsync(houseId, new DateOnly(Year, 7, 6), new DateOnly(Year, 7, 8), guests: 7);

        (await ReadValidationErrorsAsync(res, PricingErrors.Guests))
            .Should().ContainSingle().Which.Should().Be("This house sleeps at most 6 guests.");
    }

    [Fact]
    public async Task Quote_GuestsAtCapacity_Succeeds()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();

        var quote = await scenario.QuoteAsync(houseId, new DateOnly(Year, 7, 6), new DateOnly(Year, 7, 8), guests: 6);

        quote.Total.Should().Be(2 * HighRate + 4 * GuestFeePerNight * 2 + CleaningFee);
    }

    [Fact]
    public async Task Quote_NoGuests_ReturnsGuestsError()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();

        using var res = await scenario.GetQuoteAsync(houseId, new DateOnly(Year, 7, 6), new DateOnly(Year, 7, 8), guests: 0);

        await ReadValidationErrorsAsync(res, PricingErrors.Guests);
    }

    [Fact]
    public async Task Quote_TooManyGuestsOnBlockedDates_ReportsGuestsBeforeAvailability()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var arrival = new DateOnly(Year, 7, 20);
        await BlockAsync(houseId, arrival, arrival.AddDays(5));

        using (var tooMany = await scenario.GetQuoteAsync(houseId, arrival, arrival.AddDays(2), guests: 9))
        {
            await ReadValidationErrorsAsync(tooMany, PricingErrors.Guests);
        }

        using var blocked = await scenario.GetQuoteAsync(houseId, arrival, arrival.AddDays(2), guests: 2);
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Quote_StayLongerThanAYear_ReturnsDatesError()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var arrival = new DateOnly(Year, 1, 1);

        using var tooLong = await scenario.GetQuoteAsync(houseId, arrival, arrival.AddDays(366), guests: 2);
        using var reversed = await scenario.GetQuoteAsync(houseId, arrival.AddDays(3), arrival, guests: 2);

        (await ReadValidationErrorsAsync(tooLong, PricingErrors.Dates)).Single().Should().Contain("365");
        await ReadValidationErrorsAsync(reversed, PricingErrors.Dates);
    }

    [Fact]
    public async Task Quote_ArrivalInThePast_ReturnsDatesError()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        using var res = await scenario.GetQuoteAsync(houseId, yesterday, yesterday.AddDays(3), guests: 2);

        (await ReadValidationErrorsAsync(res, PricingErrors.Dates)).Single().Should().Contain("past");
    }

    [Fact]
    public async Task Quote_PostEndpoint_ReturnsSameAmountsAsGetEndpoint()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var arrival = new DateOnly(Year, 9, 7);

        using var res = await scenario.Admin.PostAsJsonAsync("api/pricing/quote", new PriceQuoteRequestDto(houseId, arrival, arrival.AddDays(4), 2, null));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await res.Content.ReadFromJsonAsync<PriceQuoteResponseDto>();

        quote!.Total.Should().Be(4 * LowRate + CleaningFee);
        quote.VatIncluded.Should().Be(quote.Total * 0.2m);
    }

    private async Task BlockAsync(Guid houseId, DateOnly start, DateOnly end)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AvailabilityBlocks.Add(new AvailabilityBlock
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            StartDate = start,
            EndDate = end,
            Status = AvailabilityStatus.Blocked,
            Source = AvailabilitySource.Manual,
            CreatedBy = "tests"
        });
        await db.SaveChangesAsync();
    }
}
