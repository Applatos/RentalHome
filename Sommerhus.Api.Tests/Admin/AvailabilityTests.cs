using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Admin;

public sealed class AvailabilityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AvailabilityTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateBlock_ValidDates_ReturnsCreated()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2030, 7, 1),
            EndDate = new DateOnly(2030, 7, 14),
            Status = AvailabilityStatus.Blocked,
            Note = "Owner vacation"
        };

        var res = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var block = await res.Content.ReadFromJsonAsync<AvailabilityBlockDto>();
        block.Should().NotBeNull();
        block!.HouseId.Should().Be(houseId);
        block.StartDate.Should().Be(new DateOnly(2030, 7, 1));
        block.EndDate.Should().Be(new DateOnly(2030, 7, 14));
        block.Status.Should().Be(AvailabilityStatus.Blocked);
        block.Note.Should().Be("Owner vacation");
        block.Source.Should().Be(AvailabilitySource.Manual);
    }

    [Fact]
    public async Task CreateBlock_EndBeforeStart_ReturnsBadRequest()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2030, 8, 15),
            EndDate = new DateOnly(2030, 8, 1),
            Status = AvailabilityStatus.Blocked
        };

        var res = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBlock_OverlappingDates_ReturnsConflict()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto1 = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2031, 3, 1),
            EndDate = new DateOnly(2031, 3, 15),
            Status = AvailabilityStatus.Blocked
        };
        var res1 = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto2 = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2031, 3, 10),
            EndDate = new DateOnly(2031, 3, 20),
            Status = AvailabilityStatus.Blocked
        };
        var res2 = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto2);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetBlocks_ReturnsBlocksInRange()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2032, 1, 1),
            EndDate = new DateOnly(2032, 1, 31),
            Status = AvailabilityStatus.Blocked,
            Note = "Maintenance"
        };
        await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);

        var res = await _client.GetAsync($"api/admin/houses/{houseId}/availability?from=2032-01-01&to=2032-02-01");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var blocks = await res.Content.ReadFromJsonAsync<List<AvailabilityBlockDto>>();
        blocks.Should().NotBeNull();
        blocks!.Should().Contain(b => b.Note == "Maintenance");
    }

    [Fact]
    public async Task GetBlocks_OutOfRange_ReturnsEmpty()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2033, 6, 1),
            EndDate = new DateOnly(2033, 6, 15),
            Status = AvailabilityStatus.Blocked
        };
        await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);

        var res = await _client.GetAsync($"api/admin/houses/{houseId}/availability?from=2033-01-01&to=2033-02-01");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var blocks = await res.Content.ReadFromJsonAsync<List<AvailabilityBlockDto>>();
        blocks.Should().NotBeNull();
        blocks!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBlock_ChangesDates()
    {
        var houseId = await GetSeededHouseIdAsync();

        var createDto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2034, 5, 1),
            EndDate = new DateOnly(2034, 5, 10),
            Status = AvailabilityStatus.Blocked,
            Note = "Before update"
        };
        var createRes = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<AvailabilityBlockDto>();

        var updateDto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2034, 5, 5),
            EndDate = new DateOnly(2034, 5, 15),
            Status = AvailabilityStatus.Tentative,
            Note = "After update"
        };
        var updateRes = await _client.PutAsJsonAsync($"api/admin/houses/{houseId}/availability/{created!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateRes.Content.ReadFromJsonAsync<AvailabilityBlockDto>();
        updated!.StartDate.Should().Be(new DateOnly(2034, 5, 5));
        updated.EndDate.Should().Be(new DateOnly(2034, 5, 15));
        updated.Status.Should().Be(AvailabilityStatus.Tentative);
        updated.Note.Should().Be("After update");
    }

    [Fact]
    public async Task DeleteBlock_Succeeds()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2035, 9, 1),
            EndDate = new DateOnly(2035, 9, 10),
            Status = AvailabilityStatus.Blocked
        };
        var createRes = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);
        var created = await createRes.Content.ReadFromJsonAsync<AvailabilityBlockDto>();

        var deleteRes = await _client.DeleteAsync($"api/admin/houses/{houseId}/availability/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _client.GetAsync($"api/admin/houses/{houseId}/availability/{created.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CheckAvailability_NoBlocks_ReturnsAvailable()
    {
        var houseId = await GetSeededHouseIdAsync();

        var res = await _client.GetAsync($"api/admin/houses/{houseId}/availability/check?checkIn=2040-01-01&checkOut=2040-01-10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AvailabilityCheckResponse>();
        body!.Available.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAvailability_WithBlock_ReturnsUnavailable()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2041, 2, 1),
            EndDate = new DateOnly(2041, 2, 15),
            Status = AvailabilityStatus.Blocked
        };
        await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto);

        var res = await _client.GetAsync($"api/admin/houses/{houseId}/availability/check?checkIn=2041-02-05&checkOut=2041-02-10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AvailabilityCheckResponse>();
        body!.Available.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBlock_NonExistentHouse_ReturnsNotFound()
    {
        var dto = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2030, 1, 1),
            EndDate = new DateOnly(2030, 1, 10),
            Status = AvailabilityStatus.Blocked
        };

        var res = await _client.PostAsJsonAsync($"api/admin/houses/{Guid.NewGuid()}/availability", dto);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjacentBlocks_NoOverlap_BothSucceed()
    {
        var houseId = await GetSeededHouseIdAsync();

        var dto1 = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2036, 4, 1),
            EndDate = new DateOnly(2036, 4, 10),
            Status = AvailabilityStatus.Blocked
        };
        var res1 = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto2 = new UpsertAvailabilityBlockDto
        {
            StartDate = new DateOnly(2036, 4, 10),
            EndDate = new DateOnly(2036, 4, 20),
            Status = AvailabilityStatus.Blocked
        };
        var res2 = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/availability", dto2);
        res2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> GetSeededHouseIdAsync()
    {
        var houses = await _client.GetFromJsonAsync<PageResult<AdminHouseListItemDto>>("api/admin/houses?pageSize=1");
        return houses!.Items.First().Id;
    }

    private sealed record AvailabilityCheckResponse(Guid HouseId, DateOnly CheckIn, DateOnly CheckOut, bool Available);
}
