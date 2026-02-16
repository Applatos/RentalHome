using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Domain.Models;
using Xunit;

namespace Sommerhus.Api.Tests.Application;

public sealed class SearchIndexerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceScopeFactory scopeFactory;

    public SearchIndexerTests(CustomWebApplicationFactory factory)
        => scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task RebuildAsync_WithExistingHouses_CreatesSearchDocuments()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();

        var house = await CreateHouseAsync(db, "SearchIndexer Rebuild House");

        await indexer.RebuildAsync(CancellationToken.None);

        var document = await db.HouseSearchDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.HouseId == house.Id);

        document.Should().NotBeNull();
        document!.Title.Should().Be("SearchIndexer Rebuild House");
        document.SearchVector.Should().Contain("SearchIndexer Rebuild House");
    }

    [Fact]
    public async Task UpdateHouseAsync_WhenHouseChanges_RefreshesSearchDocument()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();

        var house = await CreateHouseAsync(db, "SearchIndexer Initial Title");

        await indexer.UpdateHouseAsync(house.Id, CancellationToken.None);

        house.Title = "SearchIndexer Updated Title";
        house.Description = "Updated description for indexer test.";
        await db.SaveChangesAsync();

        await indexer.UpdateHouseAsync(house.Id, CancellationToken.None);

        var document = await db.HouseSearchDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.HouseId == house.Id);

        document.Should().NotBeNull();
        document!.Title.Should().Be("SearchIndexer Updated Title");
        document.Summary.Should().Contain("Updated description");
        document.SearchVector.Should().Contain("SearchIndexer Updated Title");
    }

    [Fact]
    public async Task RemoveHouseAsync_WithExistingDocument_DeletesDocument()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<ISearchIndexer>();

        var house = await CreateHouseAsync(db, "SearchIndexer Remove House");

        await indexer.UpdateHouseAsync(house.Id, CancellationToken.None);
        await indexer.RemoveHouseAsync(house.Id, CancellationToken.None);

        var document = await db.HouseSearchDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.HouseId == house.Id);

        document.Should().BeNull();
    }

    private static async Task<VacationHouse> CreateHouseAsync(AppDbContext db, string title)
    {
        var cityId = await db.Cities
            .AsNoTracking()
            .Select(c => c.Id)
            .FirstAsync();

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = title,
            CityId = cityId,
            Status = EntityStatus.Published,
            Description = "Test description"
        };

        db.Houses.Add(house);
        await db.SaveChangesAsync();
        return house;
    }
}
