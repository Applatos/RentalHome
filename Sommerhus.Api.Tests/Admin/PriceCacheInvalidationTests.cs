using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Admin;

/// <summary>
/// Every price-affecting change must invalidate cached quotes: re-quoting the same dates right
/// after the change returns the new price, never the cached one.
/// </summary>
public sealed class PriceCacheInvalidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateOnly Arrival = new(Year, 3, 2);
    private static readonly DateOnly Departure = Arrival.AddDays(3);
    private const decimal LowSeasonQuote = 3 * LowRate + CleaningFee;
    private const decimal HighSeasonQuote = 3 * HighRate + CleaningFee;

    private readonly PricingScenario scenario;

    public PriceCacheInvalidationTests(CustomWebApplicationFactory factory)
    {
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task SpanEditedOnHouseCalendarTab_RequoteReturnsNewPrice()
    {
        var groupId = await scenario.CreateGroupAsync();
        var span = await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31));
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        using var edit = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar/{span.Id}",
            Span("A", span.StartDate, span.EndDate));
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());
        (await edit.Content.ReadFromJsonAsync<SeasonSpanDto>())!.Code.Should().Be("A");

        (await QuoteTotalAsync(houseId)).Should().Be(HighSeasonQuote);
    }

    [Fact]
    public async Task SpanEditedOnGroupCalendar_RequoteReturnsNewPriceForEveryHouseInGroup()
    {
        var groupId = await scenario.CreateGroupAsync();
        var span = await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31));
        var first = await scenario.CreateHouseAsync(groupId);
        var second = await scenario.CreateHouseAsync(groupId);
        foreach (var houseId in new[] { first, second })
        {
            await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
            (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);
        }

        using var edit = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/house-groups/{groupId}/calendar/{span.Id}",
            Span("A", span.StartDate, span.EndDate));
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());

        (await QuoteTotalAsync(first)).Should().Be(HighSeasonQuote);
        (await QuoteTotalAsync(second)).Should().Be(HighSeasonQuote);
    }

    [Fact]
    public async Task SpanDeleted_RequoteReturnsUnpricedNights()
    {
        var groupId = await scenario.CreateGroupAsync();
        var span = await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31));
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["B"] = LowRate });
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        using var delete = await scenario.Admin.DeleteAsync($"api/admin/houses/{houseId}/calendar/{span.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var requote = await scenario.GetQuoteAsync(houseId, Arrival, Departure, guests: 2);
        await ReadValidationErrorsAsync(requote, PricingErrors.UnpricedNights);
    }

    [Fact]
    public async Task BulkGroupSpansReplaced_RequoteReturnsNewPrice()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31));
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        using var replace = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/calendar/groups/{groupId}/spans",
            new[] { new SeasonSpanDto(Guid.Empty, new DateOnly(Year, 3, 1), new DateOnly(Year, 3, 31), "a") });
        replace.StatusCode.Should().Be(HttpStatusCode.OK, await replace.Content.ReadAsStringAsync());
        (await replace.Content.ReadFromJsonAsync<List<SeasonSpanDto>>())!.Single().Code.Should().Be("A");

        (await QuoteTotalAsync(houseId)).Should().Be(HighSeasonQuote);
    }

    [Fact]
    public async Task PlanSaved_RequoteReturnsNewPrice()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = 700m });

        (await QuoteTotalAsync(houseId)).Should().Be(3 * 700m + CleaningFee);
    }

    [Fact]
    public async Task GroupChanged_RequoteUsesTheNewGroupsCalendar()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        var otherGroup = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(otherGroup, "A", new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));
        var dto = await scenario.HouseDtoAsync(otherGroup);
        using var update = await scenario.Admin.PutAsJsonAsync($"api/admin/houses/{houseId}", dto);
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());

        (await QuoteTotalAsync(houseId)).Should().Be(HighSeasonQuote);
    }

    [Fact]
    public async Task CalendarOverrideCreatedAndRemoved_RequoteFollowsTheEffectiveCalendar()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        using (var create = await scenario.Admin.PostAsJsonAsync($"api/admin/houses/{houseId}/calendar-override/create", new { Name = "Empty override" }))
        {
            create.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // The new override has no spans, so the cached low-season quote must not be served.
        using (var requote = await scenario.GetQuoteAsync(houseId, Arrival, Departure, guests: 2))
        {
            await ReadValidationErrorsAsync(requote, PricingErrors.UnpricedNights);
        }

        using (var remove = await scenario.Admin.DeleteAsync($"api/admin/houses/{houseId}/calendar-override"))
        {
            remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);
    }

    [Fact]
    public async Task ExistingCalendarSetAsOverride_RequoteUsesIt()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        // A second group's calendar that is high season all year serves as the override.
        var otherGroup = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(otherGroup, "A", new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));
        var calendars = await scenario.Admin.GetFromJsonAsync<List<CalendarDto>>("api/admin/calendars");
        var group = await scenario.Admin.GetFromJsonAsync<HouseGroupDto>($"api/admin/house-groups/{otherGroup}");
        var calendarId = calendars!.Single(c => c.Name == $"{group!.Name} Calendar").Id;

        using var set = await scenario.Admin.PostAsJsonAsync($"api/admin/houses/{houseId}/calendar-override", new { CalendarId = calendarId });
        set.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await QuoteTotalAsync(houseId)).Should().Be(HighSeasonQuote);
    }

    [Fact]
    public async Task PlanActivatedAndDeleted_RequoteFollowsTheActivePlan()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        var plans = await scenario.Admin.GetFromJsonAsync<List<PricePlanDetailsDto>>($"api/admin/pricing/plans/{houseId}");
        var planId = plans!.Single().PlanId;
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate }, isActive: false, planId: planId);

        using (var inactive = await scenario.GetQuoteAsync(houseId, Arrival, Departure, guests: 2))
        {
            await ReadValidationErrorsAsync(inactive, PricingErrors.UnpricedNights);
        }

        using (var activate = await scenario.Admin.PostAsync($"api/admin/pricing/plans/{planId}/activate", null))
        {
            activate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await QuoteTotalAsync(houseId)).Should().Be(LowSeasonQuote);

        using (var delete = await scenario.Admin.DeleteAsync($"api/admin/houses/{houseId}/pricing/rate-plans/{planId}"))
        {
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var deleted = await scenario.GetQuoteAsync(houseId, Arrival, Departure, guests: 2);
        await ReadValidationErrorsAsync(deleted, PricingErrors.UnpricedNights);
    }

    private async Task<decimal> QuoteTotalAsync(Guid houseId)
        => (await scenario.QuoteAsync(houseId, Arrival, Departure, guests: 2)).Total;
}
