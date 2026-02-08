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

public class AuditTests : IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly ITestOutputHelper output;

    public AuditTests(ITestOutputHelper output)
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

    [Fact]
    public async Task Create_House_GeneratesAuditEntry()
    {
        // Arrange: get a city to use
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var dto = new UpsertHouseDto { Title = "Audit Test House", Address = "Audit Address", CityId = cityId, Description = "Audit Description" };

        // Act: create a house via API
        using var response = await client.PostAsJsonAsync("/api/admin/houses", dto);
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var houseId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert: audit entry was created
        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntries = await db2.AuditEntries
            .Where(e => e.EntityType == nameof(VacationHouse) && e.EntityId == houseId.ToString())
            .ToListAsync();

        auditEntries.Should().ContainSingle();
        var entry = auditEntries.Single();
        entry.Action.Should().Be(AuditAction.Created);
        entry.ChangedBy.Should().NotBeNullOrWhiteSpace();
        entry.Changes.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Update_House_GeneratesAuditEntry()
    {
        // Arrange: create a house first
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Before Update",
            CityId = cityId,
            Address = "Old Address"
        };
        db.Houses.Add(house);
        await db.SaveChangesAsync();

        // Act: update via API
        var updateDto = new UpsertHouseDto { Title = "After Update", Address = "New Address", CityId = cityId, Description = "Updated Description" };
        using var response = await client.PutAsJsonAsync($"/api/admin/houses/{house.Id}", updateDto);
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: audit entries include an Updated action
        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var updateEntries = await db2.AuditEntries
            .Where(e => e.EntityType == nameof(VacationHouse)
                        && e.EntityId == house.Id.ToString()
                        && e.Action == AuditAction.Updated)
            .ToListAsync();

        updateEntries.Should().HaveCountGreaterThanOrEqualTo(1);
        var entry = updateEntries.First();
        entry.ChangedBy.Should().NotBeNullOrWhiteSpace();
        entry.Changes.Should().Contain("Title");
    }

    [Fact]
    public async Task Delete_House_GeneratesAuditEntry()
    {
        // Arrange: create a house first
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            CityId = cityId,
            Address = "Delete Address"
        };
        db.Houses.Add(house);
        await db.SaveChangesAsync();

        // Act: delete via API
        using var response = await client.DeleteAsync($"/api/admin/houses/{house.Id}");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: audit entries include a Deleted action
        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var deleteEntries = await db2.AuditEntries
            .Where(e => e.EntityType == nameof(VacationHouse)
                        && e.EntityId == house.Id.ToString()
                        && e.Action == AuditAction.Deleted)
            .ToListAsync();

        deleteEntries.Should().ContainSingle();
    }

    [Fact]
    public async Task AuditEndpoint_ReturnsPagedResults()
    {
        // Act: query the audit endpoint
        using var response = await client.GetAsync("/api/admin/audit?page=1&pageSize=10");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PageResult<AuditEntryDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task AuditEndpoint_FiltersByEntityType()
    {
        // Arrange: create a house to generate audit entries
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var dto = new UpsertHouseDto { Title = "Filter Test House", Address = "Filter Address", CityId = cityId, Description = "Test" };
        using var createResponse = await client.PostAsJsonAsync("/api/admin/houses", dto);
        createResponse.EnsureSuccessStatusCode();

        // Act: filter by entity type
        using var response = await client.GetAsync("/api/admin/audit?entity=VacationHouse&page=1&pageSize=50");
        await response.DumpIfError(output);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PageResult<AuditEntryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().AllSatisfy(e => e.EntityType.Should().Be("VacationHouse"));
    }

    [Fact]
    public async Task IAuditable_CreatedAtUtc_IsPopulated()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cityId = await db.Cities.Select(c => c.Id).FirstAsync();

        var dto = new UpsertHouseDto { Title = "Timestamp Test", Address = "Addr", CityId = cityId, Description = "Test" };

        // Act
        var before = DateTime.UtcNow;
        using var response = await client.PostAsJsonAsync("/api/admin/houses", dto);
        response.EnsureSuccessStatusCode();
        var houseId = await response.Content.ReadFromJsonAsync<Guid>();
        var after = DateTime.UtcNow;

        // Assert: CreatedAtUtc and CreatedBy are set
        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await db2.Houses.AsNoTracking().FirstAsync(h => h.Id == houseId);

        house.CreatedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        house.CreatedBy.Should().Be("admin");
        house.UpdatedAtUtc.Should().BeNull();
    }
}
