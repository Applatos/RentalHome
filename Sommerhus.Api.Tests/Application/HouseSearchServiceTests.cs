using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;
using Xunit;

namespace Sommerhus.Api.Tests.Application;

public sealed class HouseSearchServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceScopeFactory scopeFactory;

    public HouseSearchServiceTests(CustomWebApplicationFactory factory)
        => scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task SearchAsync_WithExactTitleQuery_ReturnsBestMatchFirst()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();
        var searchService = scope.ServiceProvider.GetRequiredService<IHouseSearchService>();

        var cityId = await db.Cities.AsNoTracking().Select(c => c.Id).FirstAsync();

        var exact = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Blue Harbor House",
            CityId = cityId,
            Status = EntityStatus.Published,
            Description = "Exact match test"
        };

        var partial = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Harbor retreat",
            CityId = cityId,
            Status = EntityStatus.Published,
            Description = "Partial match test"
        };

        db.Houses.AddRange(exact, partial);
        await db.SaveChangesAsync();

        await indexer.RebuildAsync(CancellationToken.None);

        var result = await searchService.SearchAsync(new HouseSearchFilter
        {
            Query = "Blue Harbor House",
            Sort = HouseSearchSort.Relevance,
            Page = 1,
            PageSize = 10
        }, "https://localhost", CancellationToken.None);

        result.Items.Should().NotBeEmpty();
        result.Items.First().Title.Should().Be("Blue Harbor House");
    }

    [Fact]
    public async Task SearchAsync_WithFilterCombination_ReturnsOnlyMatchingHouses()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();
        var searchService = scope.ServiceProvider.GetRequiredService<IHouseSearchService>();

        var cityId = await db.Cities.AsNoTracking().Select(c => c.Id).FirstAsync();
        var keySuffix = Guid.NewGuid().ToString("N")[..8];
        var poolKey = $"pool_{keySuffix}";
        var bedroomsKey = $"bedrooms_{keySuffix}";

        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Filter Area",
            Status = EntityStatus.Published
        };

        var poolFeature = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Pool",
            Key = poolKey,
            ValueType = FeatureValueType.Bool,
            Category = FeatureCategory.Facility,
            IsSearchable = true
        };

        var bedroomsFeature = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Bedrooms",
            Key = bedroomsKey,
            ValueType = FeatureValueType.Int,
            Category = FeatureCategory.Property,
            IsSearchable = true
        };

        db.Areas.Add(area);
        db.Features.AddRange(poolFeature, bedroomsFeature);

        var matching = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Matching House",
            CityId = cityId,
            Status = EntityStatus.Published,
            Description = "Should match all filters"
        };
        matching.Areas.Add(area);

        var nonMatching = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Non Matching House",
            CityId = cityId,
            Status = EntityStatus.Published,
            Description = "Should be filtered out"
        };

        db.Houses.AddRange(matching, nonMatching);
        await db.SaveChangesAsync();

        db.HouseFeatures.AddRange(
            new HouseFeatureValue { HouseId = matching.Id, FeatureId = poolFeature.Id, RawValue = "true" },
            new HouseFeatureValue { HouseId = matching.Id, FeatureId = bedroomsFeature.Id, RawValue = "4" },
            new HouseFeatureValue { HouseId = nonMatching.Id, FeatureId = poolFeature.Id, RawValue = "false" },
            new HouseFeatureValue { HouseId = nonMatching.Id, FeatureId = bedroomsFeature.Id, RawValue = "2" });

        db.HousePriceSummaries.AddRange(
            new HousePriceSummary { HouseId = matching.Id, MinNightlyPrice = 1500, MaxNightlyPrice = 1800, Currency = "DKK" },
            new HousePriceSummary { HouseId = nonMatching.Id, MinNightlyPrice = 700, MaxNightlyPrice = 900, Currency = "DKK" });

        await db.SaveChangesAsync();

        await indexer.RebuildAsync(CancellationToken.None);

        var result = await searchService.SearchAsync(new HouseSearchFilter
        {
            AreaId = area.Id,
            MinPrice = 1000,
            MaxPrice = 2000,
            FeatureFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [poolKey] = "true"
            },
            Page = 1,
            PageSize = 10
        }, "https://localhost", CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Matching House");
    }
}
