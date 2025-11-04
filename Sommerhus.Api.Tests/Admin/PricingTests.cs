using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Pricing.Models;
using Sommerhus.Repository;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public class PricingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly ITestOutputHelper output;
    private static readonly Guid SeededHouseId = new("5fb7097c-335c-4d07-b4fd-000004e2d28c");

    public PricingTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Quote_ReturnsPriceBreakdown()
    {
        var year = DateTime.UtcNow.Year;
        var arrival = new DateOnly(year, 1, 5);
        var departure = arrival.AddDays(3);
        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 4, null);

        using var response = await client.PostAsJsonAsync("/api/pricing/quote", request);
        await response.DumpIfError(output);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await response.Content.ReadFromJsonAsync<PriceQuoteResponseDto>();
        quote.Should().NotBeNull();
        quote!.Nights.Should().BeGreaterThan(0);
        quote.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Quote_InvalidDates_ReturnsValidationProblem()
    {
        var date = new DateOnly(DateTime.UtcNow.Year, 1, 5);
        var request = new PriceQuoteRequestDto(SeededHouseId, date, date, 2, null);

        using var response = await client.PostAsJsonAsync("/api/pricing/quote", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.ReadProblem();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRatePlan_RemovesPlan()
    {
        var planId = await SeedRatePlanAsync(factory.Services, SeededHouseId, "Temp plan");

        using var response = await client.DeleteAsync($"/api/admin/houses/{SeededHouseId}/pricing/rate-plans/{planId}");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.PricePlans.AnyAsync(p => p.Id == planId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRatePlan_WithMismatchedHouse_ReturnsBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();
        var otherHouse = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Test House",
            CityId = cityId,
            Address = "Testvej 1"
        };
        db.Houses.Add(otherHouse);
        var foreignPlan = new PricePlan
        {
            Id = Guid.NewGuid(),
            HouseId = otherHouse.Id,
            Name = "Foreign",
            Currency = "DKK"
        };
        foreignPlan.SeasonPrices.Add(new SeasonPrice
        {
            Id = Guid.NewGuid(),
            Code = "A",
            NightlyPrice = 100m
        });
        db.PricePlans.Add(foreignPlan);
        await db.SaveChangesAsync();

        using var response = await client.DeleteAsync($"/api/admin/houses/{SeededHouseId}/pricing/rate-plans/{foreignPlan.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.ReadProblem();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("Rate plan belongs to another house.");
    }

    private static async Task<Guid> SeedRatePlanAsync(IServiceProvider services, Guid houseId, string name)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = new PricePlan
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            Name = name,
            Currency = "DKK"
        };
        plan.SeasonPrices.Add(new SeasonPrice
        {
            Id = Guid.NewGuid(),
            Code = "A",
            NightlyPrice = 500m
        });
        db.PricePlans.Add(plan);
        await db.SaveChangesAsync();
        return plan.Id;
    }
}
