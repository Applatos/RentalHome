using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Services.Public.Features;
using Sommerhus.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public sealed class FeatureCategorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ITestOutputHelper output;

    public FeatureCategorizationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task CreateAsync_WithCategory_PersistsCategoryAndOptions()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminFeatureService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dto = new UpsertFeatureDto(
            "Test Bedrooms",
            "test_bedrooms_cat",
            "Int",
            Category: "Property",
            IsSearchable: true,
            Options: "[\"1\",\"2\",\"3\",\"4+\"]");

        var result = await service.CreateAsync(dto, CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Success);

        var stored = await db.Features.SingleAsync(f => f.Id == result.Value);
        stored.Category.Should().Be(FeatureCategory.Property);
        stored.IsSearchable.Should().BeTrue();
        stored.Options.Should().Be("[\"1\",\"2\",\"3\",\"4+\"]");
    }

    [Fact]
    public async Task CreateAsync_WithNullCategory_DefaultsToOther()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminFeatureService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dto = new UpsertFeatureDto("Test Default Cat", "test_default_cat", "Bool");

        var result = await service.CreateAsync(dto, CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Success);

        var stored = await db.Features.SingleAsync(f => f.Id == result.Value);
        stored.Category.Should().Be(FeatureCategory.Other);
        stored.IsSearchable.Should().BeTrue();
        stored.Options.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesCategory()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminFeatureService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var feature = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Test Update Cat",
            Key = "test_update_cat",
            ValueType = FeatureValueType.Bool,
            Category = FeatureCategory.Other,
            IsSearchable = true
        };
        db.Features.Add(feature);
        await db.SaveChangesAsync();

        var dto = new UpsertFeatureDto(
            "Test Update Cat",
            "test_update_cat",
            "Bool",
            Category: "Facility",
            IsSearchable: false);

        var result = await service.UpdateAsync(feature.Id, dto, CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Success);

        var stored = await db.Features.SingleAsync(f => f.Id == feature.Id);
        stored.Category.Should().Be(FeatureCategory.Facility);
        stored.IsSearchable.Should().BeFalse();
    }

    [Fact]
    public async Task GetSearchableAsync_ReturnsOnlySearchableFeatures_GroupedByCategory()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFeatureQueryService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var searchable = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Test Searchable",
            Key = "test_searchable_grp",
            ValueType = FeatureValueType.Bool,
            Category = FeatureCategory.Facility,
            IsSearchable = true
        };
        var notSearchable = new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Test Not Searchable",
            Key = "test_not_searchable_grp",
            ValueType = FeatureValueType.Text,
            Category = FeatureCategory.Other,
            IsSearchable = false
        };
        db.Features.AddRange(searchable, notSearchable);
        await db.SaveChangesAsync();

        var result = await service.GetSearchableAsync(CancellationToken.None);

        result.Should().ContainKey("Facility");
        result["Facility"].Should().Contain(f => f.Key == "test_searchable_grp");

        var allKeys = new List<string>();
        foreach (var group in result.Values)
            foreach (var f in group)
                allKeys.Add(f.Key);

        allKeys.Should().NotContain("test_not_searchable_grp");
    }

    [Fact]
    public async Task SearchableEndpoint_ReturnsGroupedFeatures()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("api/features/searchable");
        await response.DumpIfError(output);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, List<SearchableFeatureDto>>>();
        result.Should().NotBeNull();
        result!.Keys.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchKeywords_PersistedOnHouseCreate()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var house = await db.Houses.FirstOrDefaultAsync();
        house.Should().NotBeNull();
        house!.SearchKeywords.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SeedData_ContainsCategorizedFeatures()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var features = await db.Features.ToListAsync();

        features.Should().Contain(f => f.Category == FeatureCategory.Property);
        features.Should().Contain(f => f.Category == FeatureCategory.Facility);
        features.Should().Contain(f => f.Category == FeatureCategory.Distance);

        var bedrooms = features.SingleOrDefault(f => f.Key == "bedrooms");
        bedrooms.Should().NotBeNull();
        bedrooms!.Category.Should().Be(FeatureCategory.Property);
        bedrooms.ValueType.Should().Be(FeatureValueType.Int);
        bedrooms.Options.Should().NotBeNullOrWhiteSpace();
        bedrooms.IsSearchable.Should().BeTrue();

        var pool = features.SingleOrDefault(f => f.Key == "pool");
        pool.Should().NotBeNull();
        pool!.Category.Should().Be(FeatureCategory.Facility);
        pool.ValueType.Should().Be(FeatureValueType.Bool);
    }
}
