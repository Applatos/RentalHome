using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;
using Sommerhus.Core;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public class PricingTests : IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly ITestOutputHelper output;
    private static readonly Guid SeededHouseId = new("5fb7097c-335c-4d07-b4fd-000004e2d28c");

    public PricingTests(ITestOutputHelper output)
    {
        factory = new CustomWebApplicationFactory();
        this.output = output;
        client = factory.CreateAuthenticatedClient();
        factory.EnsureHousePublished(SeededHouseId);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    [Fact]
    public async Task Quote_ReturnsPriceBreakdown()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 1, 5);
        var departure = arrival.AddDays(3);
        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 4, null);

        using var response = await client.PostAsJsonAsync("/api/pricing/quote", request);
        await response.DumpIfError(output);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await response.Content.ReadFromJsonAsync<PriceQuoteResponseDto>();
        quote.Should().NotBeNull();

        // Seeded low season (B) is 800 a night: 3 nights + 2 extra guests * 50 * 3 nights + cleaning 950.
        quote!.Nights.Should().Be(3);
        quote.Subtotal.Should().Be(3 * 800m + 2 * 50m * 3 + 950m);
        quote.Tax.Should().Be(0m);
        quote.Total.Should().Be(3650m);
        quote.VatIncluded.Should().Be(730m);
    }

    [Fact]
    public async Task Quote_GetEndpoint_ReturnsPriceBreakdown()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 2, 5);
        var departure = arrival.AddDays(4);

        using var response = await client.GetAsync($"/api/houses/{SeededHouseId}/quote?checkIn={arrival:yyyy-MM-dd}&checkOut={departure:yyyy-MM-dd}&guests=4");
        await response.DumpIfError(output);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quote = await response.Content.ReadFromJsonAsync<PriceQuoteResponseDto>();
        quote.Should().NotBeNull();
        quote!.Nights.Should().Be(4);
    }

    [Fact]
    public async Task Quote_StoresCacheRow()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 3, 5);
        var departure = arrival.AddDays(3);
        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 4, null);

        using var response = await client.PostAsJsonAsync("/api/pricing/quote", request);
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cacheRows = await db.PriceQuotes
            .Where(q => q.HouseId == SeededHouseId && q.CheckIn == arrival && q.CheckOut == departure && q.Guests == 4)
            .ToListAsync();

        cacheRows.Should().NotBeEmpty();
        cacheRows.Should().OnlyContain(r => r.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Quote_RepeatedRequest_ReusesSingleCacheRow()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 3, 20);
        var departure = arrival.AddDays(2);
        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 2, null);

        using var firstResponse = await client.PostAsJsonAsync("/api/pricing/quote", request);
        await firstResponse.DumpIfError(output);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var secondResponse = await client.PostAsJsonAsync("/api/pricing/quote", request);
        await secondResponse.DumpIfError(output);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cacheRows = await db.PriceQuotes
            .Where(q => q.HouseId == SeededHouseId && q.CheckIn == arrival && q.CheckOut == departure && q.Guests == 2)
            .ToListAsync();

        cacheRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteRatePlan_InvalidatesQuoteCacheForHouse()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 3, 25);
        var departure = arrival.AddDays(3);
        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 4, null);

        using (var quoteResponse = await client.PostAsJsonAsync("/api/pricing/quote", request))
        {
            await quoteResponse.DumpIfError(output);
            quoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var planId = await SeedRatePlanAsync(factory.Services, SeededHouseId, "Cache invalidation plan");

        using (var deleteResponse = await client.DeleteAsync($"/api/admin/houses/{SeededHouseId}/pricing/rate-plans/{planId}"))
        {
            await deleteResponse.DumpIfError(output);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cacheRows = await db.PriceQuotes
            .Where(q => q.HouseId == SeededHouseId && q.CheckIn == arrival && q.CheckOut == departure && q.Guests == 4)
            .ToListAsync();

        cacheRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Quote_UnavailableDates_ReturnsConflict()
    {
        var year = DateTime.UtcNow.Year + 1;
        var arrival = new DateOnly(year, 4, 10);
        var departure = arrival.AddDays(5);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AvailabilityBlocks.Add(new AvailabilityBlock
            {
                Id = Guid.NewGuid(),
                HouseId = SeededHouseId,
                StartDate = arrival,
                EndDate = departure,
                Status = AvailabilityStatus.Blocked,
                Source = AvailabilitySource.Manual,
                CreatedBy = "tests"
            });
            await db.SaveChangesAsync();
        }

        var request = new PriceQuoteRequestDto(SeededHouseId, arrival, departure, 2, null);
        using var response = await client.PostAsJsonAsync("/api/pricing/quote", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Quote_InvalidDates_ReturnsValidationProblem()
    {
        var date = new DateOnly(DateTime.UtcNow.Year + 1, 1, 5);
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

        using (var seedScope = factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingSummary = await seedDb.HousePriceSummaries.FirstOrDefaultAsync(s => s.HouseId == SeededHouseId);
            if (existingSummary is null)
            {
                existingSummary = new HousePriceSummary { HouseId = SeededHouseId };
                seedDb.HousePriceSummaries.Add(existingSummary);
            }

            existingSummary.MinNightlyPrice = 500;
            existingSummary.MaxNightlyPrice = 500;
            existingSummary.Currency = "DKK";
            existingSummary.ComputedAtUtc = DateTime.UtcNow;

            await seedDb.SaveChangesAsync();
        }

        using var response = await client.DeleteAsync($"/api/admin/houses/{SeededHouseId}/pricing/rate-plans/{planId}");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.PricePlans.AnyAsync(p => p.Id == planId);
        exists.Should().BeFalse();

        var summary = await db.HousePriceSummaries.FirstOrDefaultAsync(s => s.HouseId == SeededHouseId);
        summary.Should().NotBeNull();
        summary!.MinNightlyPrice.Should().Be(800m);
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

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Values.SelectMany(v => v).Should().Contain(m => m.Contains("Rate plan belongs to another house."));
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
