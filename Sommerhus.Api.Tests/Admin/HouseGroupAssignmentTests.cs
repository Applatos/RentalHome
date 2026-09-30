using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Admin;

public sealed class HouseGroupAssignmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateOnly Arrival = new(Year, 7, 6);

    private readonly PricingScenario scenario;

    public HouseGroupAssignmentTests(CustomWebApplicationFactory factory)
    {
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task Create_WithGroupId_StoresGroupAndQuotesFromItsCalendar()
    {
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();

        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        var details = await GetDetailsAsync(houseId);
        details.GroupId.Should().Be(groupId);
        details.CalendarSource.Should().StartWith("Group:");
        details.Calendar.Should().HaveCount(3);

        var quote = await scenario.QuoteAsync(houseId, Arrival, Arrival.AddDays(7), guests: 2);
        quote.Total.Should().Be(7 * HighRate + CleaningFee);
    }

    [Fact]
    public async Task Update_WithGroupId_StoresGroupAndQuotesFromItsCalendar()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        var groupId = await scenario.CreateGroupWithStandardCalendarAsync();

        using var update = await scenario.Admin.PutAsJsonAsync($"api/admin/houses/{houseId}", await scenario.HouseDtoAsync(groupId));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());

        (await GetDetailsAsync(houseId)).GroupId.Should().Be(groupId);
        var quote = await scenario.QuoteAsync(houseId, Arrival, Arrival.AddDays(7), guests: 2);
        quote.Total.Should().Be(7 * HighRate + CleaningFee);
    }

    [Fact]
    public async Task Update_WithoutGroupId_ClearsGroup()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();

        // Null means "no group": a PUT without GroupId removes the house from its group.
        using var update = await scenario.Admin.PutAsJsonAsync($"api/admin/houses/{houseId}", await scenario.HouseDtoAsync(groupId: null));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var details = await GetDetailsAsync(houseId);
        details.GroupId.Should().BeNull();
        details.Calendar.Should().BeEmpty();

        using var quote = await scenario.GetQuoteAsync(houseId, Arrival, Arrival.AddDays(7), guests: 2);
        await ReadValidationErrorsAsync(quote, PricingErrors.UnpricedNights);
    }

    [Fact]
    public async Task CreateAndUpdate_WithUnknownGroup_ReturnBadRequest()
    {
        var unknown = Guid.NewGuid();

        using var create = await scenario.Admin.PostAsJsonAsync("api/admin/houses", await scenario.HouseDtoAsync(unknown));
        (await ReadValidationErrorsAsync(create, "groupId")).Should().ContainSingle();

        var houseId = await scenario.CreateHouseAsync(groupId: null);
        using var update = await scenario.Admin.PutAsJsonAsync($"api/admin/houses/{houseId}", await scenario.HouseDtoAsync(unknown));
        (await ReadValidationErrorsAsync(update, "groupId")).Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithUnknownArea_ReturnsBadRequest()
    {
        var dto = await scenario.HouseDtoAsync(groupId: null);
        dto.AreaIds = [Guid.NewGuid()];

        using var create = await scenario.Admin.PostAsJsonAsync("api/admin/houses", dto);

        await ReadValidationErrorsAsync(create, "AreaIds");
    }

    private async Task<AdminHouseDetailsDto> GetDetailsAsync(Guid houseId)
        => (await scenario.Admin.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}"))!;
}
