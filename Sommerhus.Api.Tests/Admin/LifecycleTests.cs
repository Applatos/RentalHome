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
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public class LifecycleTests : IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly ITestOutputHelper output;

    public LifecycleTests(ITestOutputHelper output)
    {
        factory = new CustomWebApplicationFactory();
        this.output = output;
        client = factory.CreateAuthenticatedClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private async Task<Guid> CreateDraftHouseAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var dto = new UpsertHouseDto
        {
            Title = "Lifecycle Test House",
            Address = "Test Address",
            CityId = cityId,
            Description = "Test Description"
        };

        using var response = await client.PostAsJsonAsync("/api/admin/houses", dto);
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task AddImageToHouseAsync(Guid houseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Images.Add(new HouseImage
        {
            Id = Guid.NewGuid(),
            HouseId = houseId,
            FileName = "test.jpg",
            Kind = ImageKind.Gallery
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task NewHouse_HasDraftStatus()
    {
        var houseId = await CreateDraftHouseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db.Houses.AsNoTracking().FirstAsync(h => h.Id == houseId);

        house.Status.Should().Be(EntityStatus.Draft);
        house.PublishedAtUtc.Should().BeNull();
        house.ArchivedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Publish_WithImage_Succeeds()
    {
        var houseId = await CreateDraftHouseAsync();
        await AddImageToHouseAsync(houseId);

        var before = DateTime.UtcNow;
        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db.Houses.AsNoTracking().FirstAsync(h => h.Id == houseId);

        house.Status.Should().Be(EntityStatus.Published);
        house.PublishedAtUtc.Should().NotBeNull();
        house.PublishedAtUtc!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Publish_WithoutImage_ReturnsValidationError()
    {
        var houseId = await CreateDraftHouseAsync();

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Archive_PublishedHouse_Succeeds()
    {
        var houseId = await CreateDraftHouseAsync();
        await AddImageToHouseAsync(houseId);

        await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Archived });
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db.Houses.AsNoTracking().FirstAsync(h => h.Id == houseId);

        house.Status.Should().Be(EntityStatus.Archived);
        house.ArchivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Reopen_ArchivedHouse_SetsDraft()
    {
        var houseId = await CreateDraftHouseAsync();
        await AddImageToHouseAsync(houseId);

        await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });
        await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Archived });

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Draft });
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db.Houses.AsNoTracking().FirstAsync(h => h.Id == houseId);

        house.Status.Should().Be(EntityStatus.Draft);
        house.ArchivedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task InvalidTransition_DraftToArchived_Fails()
    {
        var houseId = await CreateDraftHouseAsync();

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Archived });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SameStatus_ReturnsValidationError()
    {
        var houseId = await CreateDraftHouseAsync();

        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Draft });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublicSearch_ExcludesDraftHouses()
    {
        var houseId = await CreateDraftHouseAsync();

        using var publicClient = factory.CreateClient();
        using var response = await publicClient.GetAsync("/api/houses");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PageResult<PublicHouseListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(h => h.Id == houseId);
    }

    [Fact]
    public async Task PublicSearch_IncludesPublishedHouses()
    {
        var houseId = await CreateDraftHouseAsync();
        await AddImageToHouseAsync(houseId);

        await client.PostAsJsonAsync(
            $"/api/admin/houses/{houseId}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });

        using var publicClient = factory.CreateClient();
        using var response = await publicClient.GetAsync("/api/houses");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PageResult<PublicHouseListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(h => h.Id == houseId);
    }

    [Fact]
    public async Task PublicGetById_ReturnsDraftAsNotFound()
    {
        var houseId = await CreateDraftHouseAsync();

        using var publicClient = factory.CreateClient();
        using var response = await publicClient.GetAsync($"/api/houses/{houseId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminSearch_FiltersByStatus()
    {
        var houseId = await CreateDraftHouseAsync();

        using var response = await client.GetAsync("/api/admin/houses?status=Draft");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PageResult<AdminHouseListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(h => h.Id == houseId);
    }

    [Fact]
    public async Task AdminDetails_IncludesStatusFields()
    {
        var houseId = await CreateDraftHouseAsync();

        using var response = await client.GetAsync($"/api/admin/houses/{houseId}");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await response.Content.ReadFromJsonAsync<AdminHouseDetailsDto>();
        details.Should().NotBeNull();
        details!.Status.Should().Be(EntityStatus.Draft);
    }

    [Fact]
    public async Task NonExistentHouse_StatusChange_ReturnsNotFound()
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/admin/houses/{Guid.NewGuid()}/status",
            new ChangeStatusDto { Target = EntityStatus.Published });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
