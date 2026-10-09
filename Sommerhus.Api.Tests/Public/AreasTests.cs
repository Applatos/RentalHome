using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Public;

/// <summary>
/// The public area list and area page, against the development seed: area Blåvand with the
/// published house "Blåvand Strand 4".
/// </summary>
public sealed class AreasTests : IDisposable
{
    private readonly CustomWebApplicationFactory factory = new();

    [Fact]
    public async Task List_CountsOnlyPublishedHouses_AndSummarizesTheDescription()
    {
        var (areaId, _) = await AddDraftHouseToSeededAreaAsync();
        using var client = factory.CreateClient();

        var areas = await client.GetFromJsonAsync<List<AreaListItemDto>>("/api/areas");

        var area = areas.Should().ContainSingle(a => a.Id == areaId).Which;
        area.HouseCount.Should().Be(1);
        area.Summary.Should().Be("Hyggeligt område.");
        area.CoverUrl.Should().BeNull("the seeded area has no images");
    }

    [Fact]
    public async Task Details_ListsOnlyPublishedHouses()
    {
        var (areaId, draftId) = await AddDraftHouseToSeededAreaAsync();
        using var client = factory.CreateClient();

        var area = await client.GetFromJsonAsync<AreaDetailsDto>($"/api/areas/{areaId}");

        area!.Houses.Should().ContainSingle().Which.Title.Should().Be("Blåvand Strand 4");
        area.Houses!.Should().NotContain(h => h.Id == draftId);
    }

    private async Task<(Guid AreaId, Guid DraftId)> AddDraftHouseToSeededAreaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var area = await db.Areas.Include(a => a.Cities).SingleAsync(a => a.Name == "Blåvand");
        var draft = new VacationHouse
        {
            Title = "Unpublished house",
            CityId = area.Cities.First().Id,
            Status = EntityStatus.Draft,
            Areas = [area]
        };
        db.Houses.Add(draft);
        await db.SaveChangesAsync();
        return (area.Id, draft.Id);
    }

    public void Dispose() => factory.Dispose();
}
