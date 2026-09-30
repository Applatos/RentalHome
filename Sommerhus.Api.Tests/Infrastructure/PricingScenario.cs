using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Infrastructure;

/// <summary>
/// Builds priced houses through the admin API. The standard scenario is a fresh group whose
/// calendar covers <see cref="Year"/> with low season (B) except July–August (high season, A), a
/// published house in it that sleeps 6, and an active plan with A = 1000 and B = 600.
/// </summary>
public sealed class PricingScenario(CustomWebApplicationFactory factory, HttpClient admin)
{
    public const decimal HighRate = 1000m;
    public const decimal LowRate = 600m;
    public const decimal CleaningFee = 950m;
    public const decimal GuestFeePerNight = 50m;

    /// <summary>
    /// A year the whole scenario lies in, always in the future so stays can be booked.
    /// </summary>
    public static readonly int Year = DateTime.UtcNow.Year + 1;

    public static readonly DateOnly HighSeasonStart = new(Year, 7, 1);
    public static readonly DateOnly HighSeasonEnd = new(Year, 8, 31);

    public HttpClient Admin => admin;

    public async Task<(Guid GroupId, Guid HouseId)> CreatePricedHouseAsync()
    {
        var groupId = await CreateGroupWithStandardCalendarAsync();
        var houseId = await CreateHouseAsync(groupId);
        await SavePlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = LowRate });
        return (groupId, houseId);
    }

    public async Task<Guid> CreateGroupWithStandardCalendarAsync()
    {
        var groupId = await CreateGroupAsync();
        await AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 1, 1), new DateOnly(Year, 6, 30));
        await AddGroupSpanAsync(groupId, "A", HighSeasonStart, HighSeasonEnd);
        await AddGroupSpanAsync(groupId, "B", new DateOnly(Year, 9, 1), new DateOnly(Year, 12, 31));
        return groupId;
    }

    public async Task<Guid> CreateGroupAsync()
    {
        using var res = await admin.PostAsJsonAsync("api/admin/house-groups", new HouseGroupDto(Guid.Empty, $"Group {Guid.NewGuid():N}"));
        res.StatusCode.Should().Be(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<LookupItem>())!.Id;
    }

    public async Task<SeasonSpanDto> AddGroupSpanAsync(Guid groupId, string code, DateOnly start, DateOnly end)
    {
        using var res = await PostGroupSpanAsync(groupId, code, start, end);
        res.StatusCode.Should().Be(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<SeasonSpanDto>())!;
    }

    public Task<HttpResponseMessage> PostGroupSpanAsync(Guid groupId, string code, DateOnly start, DateOnly end)
        => admin.PostAsJsonAsync($"api/admin/house-groups/{groupId}/calendar", Span(code, start, end));

    public static UpsertSeasonSpanDto Span(string code, DateOnly start, DateOnly end)
        => new() { Code = code, StartDate = start, EndDate = end };

    public async Task<Guid> CreateHouseAsync(Guid? groupId, int? maxGuests = 6, string? title = null)
    {
        using var res = await admin.PostAsJsonAsync("api/admin/houses", await HouseDtoAsync(groupId, title));
        res.StatusCode.Should().Be(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        var houseId = await res.Content.ReadFromJsonAsync<Guid>();

        if (maxGuests is not null)
        {
            await SetMaxGuestsAsync(houseId, maxGuests.Value);
        }

        await PublishAsync(houseId);
        return houseId;
    }

    public async Task<UpsertHouseDto> HouseDtoAsync(Guid? groupId, string? title = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        return new UpsertHouseDto
        {
            Title = title ?? $"Pricing house {Guid.NewGuid():N}",
            Address = "Prisvej 1",
            CityId = cityId,
            Description = "A house for pricing tests.",
            GroupId = groupId
        };
    }

    public async Task SetMaxGuestsAsync(Guid houseId, int maxGuests)
    {
        Guid featureId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            featureId = await db.Features.Where(f => f.Key == "max_guests").Select(f => f.Id).FirstAsync();
        }

        using var res = await admin.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/features",
            new[] { new PostFeatureValueDto(featureId, maxGuests.ToString(CultureInfo.InvariantCulture)) });
        res.StatusCode.Should().Be(HttpStatusCode.NoContent, await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Publishes directly in the database (the lifecycle requires images) and reindexes, so the
    /// house is quotable and appears in public search.
    /// </summary>
    public async Task PublishAsync(Guid houseId)
    {
        factory.EnsureHousePublished(houseId);

        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISearchIndexer>().UpdateHouseAsync(houseId, CancellationToken.None);
    }

    public async Task SavePlanAsync(Guid houseId, Dictionary<string, decimal> prices, bool isActive = true, Guid? planId = null)
    {
        using var res = await PutPlanAsync(houseId, prices, isActive, planId);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent, await res.Content.ReadAsStringAsync());
    }

    public Task<HttpResponseMessage> PutPlanAsync(Guid houseId, Dictionary<string, decimal> prices, bool isActive = true, Guid? planId = null)
    {
        var dto = new PricePlanDetailsDto(
            planId ?? Guid.Empty,
            houseId,
            "Standard",
            "DKK",
            isActive,
            DateTime.UtcNow,
            null,
            prices.Select(p => new SeasonPriceDto(Guid.Empty, Guid.Empty, p.Key, p.Value)).ToList());

        return admin.PutAsJsonAsync($"api/admin/houses/{houseId}/pricing", dto);
    }

    public Task<HttpResponseMessage> GetQuoteAsync(Guid houseId, DateOnly arrival, DateOnly departure, int guests)
        => admin.GetAsync($"api/houses/{houseId}/quote?checkIn={arrival:yyyy-MM-dd}&checkOut={departure:yyyy-MM-dd}&guests={guests}");

    public async Task<PriceQuoteResponseDto> QuoteAsync(Guid houseId, DateOnly arrival, DateOnly departure, int guests)
    {
        using var res = await GetQuoteAsync(houseId, arrival, departure, guests);
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<PriceQuoteResponseDto>())!;
    }

    /// <summary>
    /// Asserts a 400 validation problem and returns the messages under <paramref name="key"/>.
    /// </summary>
    public static async Task<string[]> ReadValidationErrorsAsync(HttpResponseMessage res, string key)
    {
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest, await res.Content.ReadAsStringAsync());
        var problem = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey(key);
        return problem.Errors[key];
    }

    public async Task<int> CountCachedQuotesAsync(Guid houseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PriceQuotes.CountAsync(q => q.HouseId == houseId);
    }

    public async Task<HouseSearchDocument?> GetSearchDocumentAsync(Guid houseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.HouseSearchDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.HouseId == houseId);
    }
}
