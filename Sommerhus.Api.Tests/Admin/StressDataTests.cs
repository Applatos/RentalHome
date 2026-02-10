using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Data;
using Sommerhus.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public sealed class StressDataTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ITestOutputHelper output;

    public StressDataTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task SeedAsync_ProducesValidData_WithCorrectRelationships()
    {
        using var scope = scopeFactory.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IStressDataGenerator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var options = new StressDataOptions { HouseCount = 20, AreaCount = 5, CalendarCount = 2 };
        var result = await generator.SeedAsync(options, CancellationToken.None);

        result.Success.Should().BeTrue(result.Message);
        result.EntitiesAffected.Should().BeGreaterThan(0);

        // Verify houses created
        var stressHouses = await db.Houses.Where(h => h.CreatedBy == "__stress__").ToListAsync();
        stressHouses.Should().HaveCount(20);

        // Verify each house has a valid city
        foreach (var house in stressHouses)
        {
            house.CityId.Should().NotBeEmpty();
            var city = await db.Cities.FindAsync(house.CityId);
            city.Should().NotBeNull();
        }

        // Verify feature values exist for stress houses
        var featureValues = await db.HouseFeatures
            .Where(hf => stressHouses.Select(h => h.Id).Contains(hf.HouseId))
            .ToListAsync();
        featureValues.Should().NotBeEmpty();
        featureValues.Count.Should().BeGreaterThan(stressHouses.Count * 4); // at least 4 features per house (bedrooms, bathrooms, guests, size)

        // Verify price plans exist
        var plans = await db.PricePlans
            .Where(p => p.CreatedBy == "__stress__")
            .Include(p => p.SeasonPrices)
            .ToListAsync();
        plans.Should().HaveCount(20);
        plans.Should().OnlyContain(p => p.SeasonPrices.Count >= 2);

        // Verify availability blocks exist
        var blocks = await db.AvailabilityBlocks
            .Where(a => a.CreatedBy == "__stress__")
            .ToListAsync();
        blocks.Should().NotBeEmpty();

        // Verify areas created
        var stressAreas = await db.Areas.Where(a => a.CreatedBy == "__stress__").ToListAsync();
        stressAreas.Should().HaveCount(5);

        // Verify calendars created
        var stressCals = await db.SeasonCalendars.Where(c => c.CreatedBy == "__stress__").ToListAsync();
        stressCals.Should().HaveCount(2);
    }

    [Fact]
    public async Task SeedAsync_WhenAlreadySeeded_ReturnsConflict()
    {
        using var scope = scopeFactory.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IStressDataGenerator>();

        // First seed should succeed (or already exist from previous test)
        var first = await generator.SeedAsync(new StressDataOptions { HouseCount = 10 }, CancellationToken.None);

        // Second seed should fail
        var second = await generator.SeedAsync(new StressDataOptions { HouseCount = 10 }, CancellationToken.None);
        second.Success.Should().BeFalse();
        second.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task ClearAsync_RemovesAllStressData()
    {
        using var scope = scopeFactory.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IStressDataGenerator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure stress data exists
        var seedResult = await generator.SeedAsync(new StressDataOptions { HouseCount = 10, AreaCount = 3, CalendarCount = 1 }, CancellationToken.None);
        if (!seedResult.Success)
        {
            // Already seeded from previous test, that's fine
        }

        var clearResult = await generator.ClearAsync(CancellationToken.None);
        clearResult.Success.Should().BeTrue();

        // Verify stress data is gone
        var stressHouses = await db.Houses.CountAsync(h => h.CreatedBy == "__stress__");
        stressHouses.Should().Be(0);

        var stressAreas = await db.Areas.CountAsync(a => a.CreatedBy == "__stress__");
        stressAreas.Should().Be(0);

        var stressCals = await db.SeasonCalendars.CountAsync(c => c.CreatedBy == "__stress__");
        stressCals.Should().Be(0);

        // Verify original seed data still exists
        var originalHouse = await db.Houses.AnyAsync(h => h.Id == new Guid("5fb7097c-335c-4d07-b4fd-000004e2d28c"));
        originalHouse.Should().BeTrue("original seed data should not be deleted");
    }

    [Fact]
    public async Task StressEndpoint_RequiresAuth()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsync("api/admin/stress/seed", null);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StressEndpoint_Seed_ReturnsOk()
    {
        var client = factory.CreateAuthenticatedClient();

        // Clear first in case previous tests left data
        await client.DeleteAsync("api/admin/stress/clear");

        var res = await client.PostAsync("api/admin/stress/seed?houses=10", null);
        await res.DumpIfError(output);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await res.Content.ReadFromJsonAsync<StressDataResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.EntitiesAffected.Should().BeGreaterThan(0);

        // Clean up
        var clearRes = await client.DeleteAsync("api/admin/stress/clear");
        clearRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
