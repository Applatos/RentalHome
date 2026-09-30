using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Tests.Admin;

public sealed class CalendarTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CalendarTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ListCalendars_ReturnsSeededCalendar()
    {
        var res = await _client.GetAsync("api/admin/calendars");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var calendars = await res.Content.ReadFromJsonAsync<List<CalendarDto>>();
        calendars.Should().NotBeNull();
        calendars!.Count.Should().BeGreaterThanOrEqualTo(1);
        calendars.Should().Contain(c => c.Name == "Vesterhavet Calendar");
    }

    [Fact]
    public async Task CreateCalendar_ReturnsNewCalendar()
    {
        var dto = new UpsertCalendarDto { Name = "Test Calendar 2027", Year = 2027, IsTemplate = false };
        var res = await _client.PostAsJsonAsync("api/admin/calendars", dto);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await res.Content.ReadFromJsonAsync<CalendarDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Test Calendar 2027");
        created.Year.Should().Be(2027);
        created.SpanCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCalendar_ChangesName()
    {
        var createDto = new UpsertCalendarDto { Name = "Before Update" };
        var createRes = await _client.PostAsJsonAsync("api/admin/calendars", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<CalendarDto>();

        var updateDto = new UpsertCalendarDto { Name = "After Update", IsTemplate = true };
        var updateRes = await _client.PutAsJsonAsync($"api/admin/calendars/{created!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateRes.Content.ReadFromJsonAsync<CalendarDto>();
        updated!.Name.Should().Be("After Update");
        updated.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCalendar_UnusedCalendar_Succeeds()
    {
        var dto = new UpsertCalendarDto { Name = "To Delete" };
        var createRes = await _client.PostAsJsonAsync("api/admin/calendars", dto);
        var created = await createRes.Content.ReadFromJsonAsync<CalendarDto>();

        var deleteRes = await _client.DeleteAsync($"api/admin/calendars/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _client.GetAsync($"api/admin/calendars/{created.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCalendar_UsedByGroup_Fails()
    {
        // The seeded "Vesterhavet Calendar" is used by group "Vesterhavet"
        var calendarsRes = await _client.GetFromJsonAsync<List<CalendarDto>>("api/admin/calendars");
        var seededCal = calendarsRes!.First(c => c.Name == "Vesterhavet Calendar");

        var deleteRes = await _client.DeleteAsync($"api/admin/calendars/{seededCal.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HouseDetails_ShowsCalendarSource()
    {
        // Seeded house is in group "Vesterhavet" which has a default calendar
        var houseId = await GetSeededHouseIdAsync();

        // Ensure no override is set (clean up from other tests)
        await _client.DeleteAsync($"api/admin/houses/{houseId}/calendar-override");

        var res = await _client.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        res.Should().NotBeNull();
        res!.CalendarSource.Should().Contain("Vesterhavet");
        res.CalendarOverrideId.Should().BeNull();
        res.Calendar.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateHouseOverride_CreatesCalendarAndSetsOverride()
    {
        var houseId = await GetSeededHouseIdAsync();

        var res = await _client.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar-override/create",
            new { Name = "Custom for test house" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var cal = await res.Content.ReadFromJsonAsync<CalendarDto>();
        cal.Should().NotBeNull();
        cal!.Name.Should().Be("Custom for test house");

        // House details should now show override
        var details = await _client.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        details!.CalendarOverrideId.Should().Be(cal.Id);
        details.CalendarSource.Should().Contain("Custom");
        details.Calendar.Should().BeEmpty(); // New calendar has no spans
    }

    [Fact]
    public async Task CreateHouseOverride_WithoutName_UsesDefaultName()
    {
        var houseId = await GetSeededHouseIdAsync();

        // The admin form's name field is optional, so a blank field arrives as null.
        var res = await _client.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar-override/create",
            new { Name = (string?)null });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var cal = await res.Content.ReadFromJsonAsync<CalendarDto>();
        cal!.Name.Should().Contain("Blåvand Strand 4");

        (await _client.DeleteAsync($"api/admin/houses/{houseId}/calendar-override"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveHouseOverride_RevertsToGroupCalendar()
    {
        var houseId = await GetSeededHouseIdAsync();

        // Set an override first
        await _client.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar-override/create",
            new { Name = "Temp override" });

        // Remove it
        var removeRes = await _client.DeleteAsync($"api/admin/houses/{houseId}/calendar-override");
        removeRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Should revert to group calendar
        var details = await _client.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        details!.CalendarOverrideId.Should().BeNull();
        details.CalendarSource.Should().Contain("Vesterhavet");
        details.Calendar.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetHouseOverride_ExistingCalendar_Works()
    {
        var houseId = await GetSeededHouseIdAsync();

        // Create a standalone calendar
        var calRes = await _client.PostAsJsonAsync("api/admin/calendars",
            new UpsertCalendarDto { Name = "Shared Override Cal" });
        var cal = await calRes.Content.ReadFromJsonAsync<CalendarDto>();

        // Set it as override
        var setRes = await _client.PostAsJsonAsync(
            $"api/admin/houses/{houseId}/calendar-override",
            new { CalendarId = cal!.Id });
        setRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var details = await _client.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        details!.CalendarOverrideId.Should().Be(cal.Id);

        // Clean up: remove override
        await _client.DeleteAsync($"api/admin/houses/{houseId}/calendar-override");
    }

    [Fact]
    public async Task SeasonSpanCrud_OnGroupCalendar_Works()
    {
        // Get the seeded house's group
        var houseId = await GetSeededHouseIdAsync();
        var details = await _client.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        var initialCount = details!.Calendar?.Count ?? 0;

        // Add a span via the house endpoint
        var addDto = new UpsertSeasonSpanDto { Code = "A", StartDate = new DateOnly(2030, 1, 1), EndDate = new DateOnly(2030, 1, 31) };
        var addRes = await _client.PostAsJsonAsync($"api/admin/houses/{houseId}/calendar", addDto);
        addRes.EnsureSuccessStatusCode();

        var span = await addRes.Content.ReadFromJsonAsync<SeasonSpanDto>();
        span.Should().NotBeNull();
        span!.Code.Should().Be("A");

        // Delete the span
        var deleteRes = await _client.DeleteAsync($"api/admin/houses/{houseId}/calendar/{span.Id}");
        deleteRes.EnsureSuccessStatusCode();
    }

    private async Task<Guid> GetSeededHouseIdAsync()
    {
        var houses = await _client.GetFromJsonAsync<PageResult<AdminHouseListItemDto>>("api/admin/houses?pageSize=1");
        return houses!.Items.First().Id;
    }
}
