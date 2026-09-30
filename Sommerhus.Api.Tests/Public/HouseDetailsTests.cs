using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Tests.Public;

public sealed class HouseDetailsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid SeededHouseId = new("5fb7097c-335c-4d07-b4fd-000004e2d28c");

    private readonly CustomWebApplicationFactory factory;
    private readonly PricingScenario scenario;
    private readonly HttpClient publicClient;

    public HouseDetailsTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
        publicClient = factory.CreateClient();
    }

    [Fact]
    public async Task Get_SeededHouse_ReturnsCapacityAndFeaturesInSortOrder()
    {
        var details = await publicClient.GetFromJsonAsync<PublicHouseDetailsDto>($"api/houses/{SeededHouseId}");

        details!.MaxGuests.Should().Be(6);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expectedOrder = await db.HouseFeatures
            .Where(hf => hf.HouseId == SeededHouseId)
            .OrderBy(hf => hf.Feature!.SortOrder)
            .Select(hf => hf.FeatureId)
            .ToListAsync();

        expectedOrder.Should().HaveCountGreaterThan(1);
        details.Features.Select(f => f.Id).Should().Equal(expectedOrder);
    }

    [Fact]
    public async Task Get_HouseWithoutCapacityFeature_HasNoMaxGuests()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null, maxGuests: null);

        var details = await publicClient.GetFromJsonAsync<PublicHouseDetailsDto>($"api/houses/{houseId}");

        details!.MaxGuests.Should().BeNull();
    }
}
