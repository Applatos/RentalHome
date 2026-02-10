using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Public;

public sealed class AvailabilityTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _authClient;
    private readonly HttpClient _anonClient;

    public AvailabilityTests()
    {
        _factory = new CustomWebApplicationFactory();
        _authClient = _factory.CreateAuthenticatedClient();
        _anonClient = _factory.CreateClient();
    }

    public void Dispose()
    {
        _authClient.Dispose();
        _anonClient.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PublicGetBlocks_PublishedHouse_ReturnsBlocks()
    {
        var houseId = await GetPublishedHouseIdAsync();

        // Create a block via admin
        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2037, 1, 1),
            EndDate = new DateOnly(2037, 1, 15),
            Status = AvailabilityStatus.Blocked,
            Note = "Secret admin note"
        };
        var createRes = await _authClient.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Query via public endpoint
        var res = await _anonClient.GetAsync($"api/houses/{houseId}/availability?from=2037-01-01&to=2037-02-01");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var blocks = await res.Content.ReadFromJsonAsync<List<AvailabilityBlockDto>>();
        blocks.Should().NotBeNull();
        blocks!.Should().Contain(b => b.StartDate == new DateOnly(2037, 1, 1));

        // Public endpoint should not expose Note or CreatedBy
        var publicBlock = blocks.First(b => b.StartDate == new DateOnly(2037, 1, 1));
        publicBlock.Note.Should().BeNull();
        publicBlock.CreatedBy.Should().BeNull();
    }

    [Fact]
    public async Task PublicGetBlocks_NonExistentHouse_ReturnsNotFound()
    {
        var res = await _anonClient.GetAsync($"api/houses/{Guid.NewGuid()}/availability?from=2037-01-01&to=2037-02-01");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> GetPublishedHouseIdAsync()
    {
        var houses = await _authClient.GetFromJsonAsync<PageResult<AdminHouseListItemDto>>("api/admin/houses?pageSize=1");
        var houseId = houses!.Items.First().Id;

        // Add an image and publish directly via DB (publishing requires at least one image)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db.Houses.Include(h => h.Images).FirstAsync(h => h.Id == houseId);

        if (house.Images.Count == 0)
        {
            db.Images.Add(new HouseImage
            {
                Id = Guid.NewGuid(),
                HouseId = houseId,
                FileName = "test.jpg",
                Kind = ImageKind.Gallery
            });
        }

        house.Status = EntityStatus.Published;
        house.PublishedAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync();

        return houseId;
    }
}
