using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Admin;

public sealed class SeasonSpanValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateOnly SummerStart = new(Year, 6, 1);
    private static readonly DateOnly SummerEnd = new(Year, 8, 31);

    private readonly PricingScenario scenario;

    public SeasonSpanValidationTests(CustomWebApplicationFactory factory)
    {
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task AddGroupSpan_InsideAnotherSpan_ReturnsBadRequestAndKeepsPrices()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", SummerStart, SummerEnd);
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });

        using var res = await scenario.PostGroupSpanAsync(groupId, "B", new DateOnly(Year, 7, 1), new DateOnly(Year, 7, 31));

        (await ReadValidationErrorsAsync(res, "startDate")).Single()
            .Should().Contain($"overlaps the period {SummerStart:yyyy-MM-dd}–{SummerEnd:yyyy-MM-dd}");
        var quote = await scenario.QuoteAsync(houseId, new DateOnly(Year, 7, 10), new DateOnly(Year, 7, 12), guests: 2);
        quote.Total.Should().Be(2 * HighRate + CleaningFee);
    }

    [Fact]
    public async Task AddHouseSpan_OverlappingEdge_ReturnsBadRequest()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", SummerStart, SummerEnd);
        var houseId = await scenario.CreateHouseAsync(groupId);

        using var overlapping = await scenario.Admin.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar",
            Span("B", new DateOnly(Year, 8, 31), new DateOnly(Year, 9, 30)));
        using var adjacent = await scenario.Admin.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar",
            Span("B", new DateOnly(Year, 9, 1), new DateOnly(Year, 9, 30)));

        await ReadValidationErrorsAsync(overlapping, "startDate");
        adjacent.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateSpan_OntoAnotherSpan_ReturnsBadRequest()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", SummerStart, SummerEnd);
        var autumn = await scenario.AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 9, 1), new DateOnly(Year, 9, 30));
        var houseId = await scenario.CreateHouseAsync(groupId);

        using var viaGroup = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/house-groups/{groupId}/calendar/{autumn.Id}",
            Span("B", new DateOnly(Year, 8, 15), new DateOnly(Year, 9, 30)));
        using var viaHouse = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar/{autumn.Id}",
            Span("B", new DateOnly(Year, 8, 15), new DateOnly(Year, 9, 30)));

        await ReadValidationErrorsAsync(viaGroup, "startDate");
        await ReadValidationErrorsAsync(viaHouse, "startDate");
    }

    [Fact]
    public async Task UpdateSpan_WithinItsOwnPeriod_Succeeds()
    {
        var groupId = await scenario.CreateGroupAsync();
        var summer = await scenario.AddGroupSpanAsync(groupId, "A", SummerStart, SummerEnd);
        var houseId = await scenario.CreateHouseAsync(groupId);

        using var res = await scenario.Admin.PutAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar/{summer.Id}",
            Span("a", new DateOnly(Year, 6, 15), SummerEnd));

        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var updated = await res.Content.ReadFromJsonAsync<SeasonSpanDto>();
        updated!.StartDate.Should().Be(new DateOnly(Year, 6, 15));
        updated.Code.Should().Be("A");
        updated.SeasonName.Should().Be("Højsæson");
    }

    [Fact]
    public async Task AddSpan_EndBeforeStart_ReturnsBadRequest()
    {
        var groupId = await scenario.CreateGroupAsync();

        using var res = await scenario.PostGroupSpanAsync(groupId, "A", SummerEnd, SummerStart);

        await ReadValidationErrorsAsync(res, "endDate");
    }

    [Fact]
    public async Task AddSpan_UnknownCode_ReturnsBadRequest()
    {
        var groupId = await scenario.CreateGroupAsync();

        using var res = await scenario.PostGroupSpanAsync(groupId, "ZZ", SummerStart, SummerEnd);

        await ReadValidationErrorsAsync(res, "code");
    }

    [Fact]
    public async Task AddSpan_LowercaseCode_IsStoredUpperCase()
    {
        var groupId = await scenario.CreateGroupAsync();

        var span = await scenario.AddGroupSpanAsync(groupId, " b ", SummerStart, SummerEnd);

        span.Code.Should().Be("B");
        span.SeasonName.Should().Be("Lavsæson");
    }

    [Fact]
    public async Task BulkUpsert_OverlappingSpans_ReturnsBadRequest()
    {
        var groupId = await scenario.CreateGroupAsync();

        using var res = await scenario.Admin.PutAsJsonAsync($"api/admin/calendar/groups/{groupId}/spans", new[]
        {
            new SeasonSpanDto(Guid.Empty, SummerStart, SummerEnd, "A"),
            new SeasonSpanDto(Guid.Empty, new DateOnly(Year, 7, 1), new DateOnly(Year, 7, 31), "B")
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
