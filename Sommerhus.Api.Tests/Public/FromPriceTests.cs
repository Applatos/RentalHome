using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Pricing;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Public;

/// <summary>
/// The "from" price on a house card: the lowest nightly price of the active plan among the season
/// codes the house's calendar uses from today on. Read through public search.
/// </summary>
public sealed class FromPriceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly PricingScenario scenario;
    private readonly HttpClient publicClient;

    public FromPriceTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
        publicClient = factory.CreateClient();
    }

    [Fact]
    public async Task FromPrice_CalendarWithBothSeasons_IsTheLowestSeasonPrice()
    {
        var title = UniqueTitle("both");
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        (await FromPriceAsync(title)).Should().Be(LowRate);
        var document = await scenario.GetSearchDocumentAsync(houseId);
        document!.MinNightlyPrice.Should().Be(LowRate);
        document.MaxNightlyPrice.Should().Be(HighRate);
    }

    [Fact]
    public async Task FromPrice_IgnoresSeasonsOnlyInPastSpans()
    {
        var title = UniqueTitle("past");
        var groupId = await scenario.CreateGroupAsync();
        var lastYear = DateTime.UtcNow.Year - 1;
        await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(lastYear, 1, 1), new DateOnly(lastYear, 12, 31));
        await scenario.AddGroupSpanAsync(groupId, "A", new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));
        var houseId = await scenario.CreateHouseAsync(groupId, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        (await FromPriceAsync(title)).Should().Be(HighRate);
    }

    [Fact]
    public async Task FromPrice_IgnoresPlanPricesForSeasonsNotInTheCalendar()
    {
        var title = UniqueTitle("unused");
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", new DateOnly(Year, 1, 1), new DateOnly(Year, 12, 31));
        var houseId = await scenario.CreateHouseAsync(groupId, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        (await FromPriceAsync(title)).Should().Be(HighRate);
    }

    [Fact]
    public async Task FromPrice_HouseWithoutCalendar_HasNone()
    {
        var title = UniqueTitle("nocalendar");
        var houseId = await scenario.CreateHouseAsync(groupId: null, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        (await FromPriceAsync(title)).Should().BeNull();
        (await scenario.GetSearchDocumentAsync(houseId))!.MinNightlyPrice.Should().BeNull();
    }

    [Fact]
    public async Task FromPrice_FollowsTheActivePlan()
    {
        var title = UniqueTitle("active");
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate }, isActive: false);
        (await FromPriceAsync(title)).Should().BeNull();

        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = 450m }, isActive: true);
        (await FromPriceAsync(title)).Should().Be(450m);
    }

    [Fact]
    public async Task FromPrice_FollowsGroupChange()
    {
        var title = UniqueTitle("regroup");
        var houseId = await scenario.CreateHouseAsync(groupId: null, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        (await FromPriceAsync(title)).Should().BeNull();

        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        using var update = await scenario.Admin.PutAsJsonAsync($"api/admin/houses/{houseId}", await scenario.HouseDtoAsync(groupId, title));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await FromPriceAsync(title)).Should().Be(LowRate);
    }

    [Fact]
    public async Task Search_SortedByPrice_OrdersBothWaysWithUnpricedHousesLast()
    {
        var token = UniqueTitle("sort");
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var cheap = await scenario.CreateHouseAsync(groupId, title: $"{token} cheap");
        var dear = await scenario.CreateHouseAsync(groupId, title: $"{token} dear");
        var middle = await scenario.CreateHouseAsync(groupId, title: $"{token} middle");
        var unpriced = await scenario.CreateHouseAsync(groupId, title: $"{token} unpriced");
        await scenario.SavePlanAsync(cheap, new() { ["A"] = 700m, ["B"] = 500.5m });
        await scenario.SavePlanAsync(dear, new() { ["A"] = 2500m, ["B"] = 1500m });
        await scenario.SavePlanAsync(middle, new() { ["A"] = 1200m, ["B"] = 900m });

        var ascending = await SearchAsync(token, "PriceAsc");
        var descending = await SearchAsync(token, "PriceDesc");

        ascending.Select(i => i.Id).Should().Equal(cheap, middle, dear, unpriced);
        ascending.Select(i => i.MinNightlyPrice).Should().Equal(500.5m, 900m, 1500m, null);
        descending.Select(i => i.Id).Should().Equal(dear, middle, cheap, unpriced);
    }

    [Fact]
    public async Task PriceChange_UpdatesOnlyThePriceColumnsOfTheSearchDocument()
    {
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId, title: UniqueTitle("stamp"));
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        var before = await scenario.GetSearchDocumentAsync(houseId);

        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate - 100m });

        // The public list orders by the document's timestamp; a price change must not move the house.
        var after = await scenario.GetSearchDocumentAsync(houseId);
        after!.MinNightlyPrice.Should().Be(LowRate - 100m);
        after.UpdatedAtUtc.Should().Be(before!.UpdatedAtUtc);
    }

    [Fact]
    public async Task RefreshAll_RepairsAStaleFromPrice()
    {
        var title = UniqueTitle("refresh");
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();
        var houseId = await scenario.CreateHouseAsync(groupId, title: title);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        using (var scope = factory.Services.CreateScope())
        {
            // A from-price written by an older rule, as in a database from before this change.
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.HousePriceSummaries.Where(s => s.HouseId == houseId)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.MinNightlyPrice, 1m));
            await db.HouseSearchDocuments.Where(d => d.HouseId == houseId)
                .ExecuteUpdateAsync(set => set.SetProperty(d => d.MinNightlyPrice, 1m));
        }
        (await FromPriceAsync(title)).Should().Be(1m);

        var refresh = new PriceSummaryRefreshService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PriceSummaryRefreshService>.Instance);
        await refresh.RefreshAllAsync(CancellationToken.None);

        (await FromPriceAsync(title)).Should().Be(LowRate);
    }

    private static string UniqueTitle(string label) => $"Fromprice{label}{Guid.NewGuid():N}"[..40];

    private async Task<decimal?> FromPriceAsync(string title)
        => (await SearchAsync(title, "Relevance")).Should().ContainSingle().Subject.MinNightlyPrice;

    private async Task<IReadOnlyList<PublicHouseListItemDto>> SearchAsync(string query, string sort)
    {
        using var res = await publicClient.GetAsync($"api/houses?q={Uri.EscapeDataString(query)}&sort={sort}");
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<PageResult<PublicHouseListItemDto>>())!.Items;
    }
}
