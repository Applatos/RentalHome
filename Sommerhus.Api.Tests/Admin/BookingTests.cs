using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Admin;

public sealed class BookingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;

    public BookingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task UserCreate_ValidBooking_ReturnsSuccess()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser1");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2040, 6, 1),
            CheckOut = new DateOnly(2040, 6, 8),
            Guests = 4,
            GuestNote = "Looking forward to it!"
        };

        var res = await userClient.PostAsJsonAsync("api/bookings", dto);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var booking = await res.Content.ReadFromJsonAsync<BookingDto>();
        booking.Should().NotBeNull();
        booking!.HouseId.Should().Be(houseId);
        booking.CheckIn.Should().Be(new DateOnly(2040, 6, 1));
        booking.CheckOut.Should().Be(new DateOnly(2040, 6, 8));
        booking.Guests.Should().Be(4);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.GuestNote.Should().Be("Looking forward to it!");
    }

    [Fact]
    public async Task UserCreate_InvalidDates_ReturnsBadRequest()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser2");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2040, 7, 10),
            CheckOut = new DateOnly(2040, 7, 5),
            Guests = 2
        };

        var res = await userClient.PostAsJsonAsync("api/bookings", dto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UserList_ReturnsOwnBookings()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser3");

        var createDto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2041, 1, 1),
            CheckOut = new DateOnly(2041, 1, 5),
            Guests = 2
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var res = await userClient.GetAsync("api/bookings");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var bookings = await res.Content.ReadFromJsonAsync<List<BookingListItemDto>>();
        bookings.Should().NotBeNull();
        bookings!.Should().Contain(b => b.HouseId == houseId);
    }

    [Fact]
    public async Task UserCancel_PendingBooking_Succeeds()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser4");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2042, 3, 1),
            CheckOut = new DateOnly(2042, 3, 7),
            Guests = 3
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createRes.Content.ReadFromJsonAsync<BookingDto>();

        var cancelRes = await userClient.PostAsync($"api/bookings/{created!.Id}/cancel", null);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await userClient.GetAsync($"api/bookings/{created.Id}");
        var cancelled = await getRes.Content.ReadFromJsonAsync<BookingDto>();
        cancelled!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task AdminList_ReturnsAllBookings()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser5");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2043, 5, 1),
            CheckOut = new DateOnly(2043, 5, 10),
            Guests = 2
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var res = await _adminClient.GetAsync("api/admin/bookings");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var bookings = await res.Content.ReadFromJsonAsync<List<BookingListItemDto>>();
        bookings.Should().NotBeNull();
        bookings!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AdminUpdateStatus_ConfirmBooking_CreatesAvailabilityBlock()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser6");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2044, 8, 1),
            CheckOut = new DateOnly(2044, 8, 14),
            Guests = 4
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createRes.Content.ReadFromJsonAsync<BookingDto>();

        var statusDto = new UpdateBookingStatusDto
        {
            Status = BookingStatus.Confirmed,
            Note = "Confirmed by admin"
        };
        var updateRes = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{created!.Id}/status", statusDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _adminClient.GetAsync($"api/admin/bookings/{created.Id}");
        var confirmed = await getRes.Content.ReadFromJsonAsync<BookingDto>();
        confirmed!.Status.Should().Be(BookingStatus.Confirmed);
        confirmed.AvailabilityBlockId.Should().NotBeNull();
        confirmed.ConfirmedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AdminUpdateStatus_CancelConfirmed_RemovesAvailabilityBlock()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser7");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2045, 2, 1),
            CheckOut = new DateOnly(2045, 2, 10),
            Guests = 2
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createRes.Content.ReadFromJsonAsync<BookingDto>();

        var confirmRes = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{created!.Id}/status",
            new UpdateBookingStatusDto { Status = BookingStatus.Confirmed });
        confirmRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cancelDto = new UpdateBookingStatusDto
        {
            Status = BookingStatus.Cancelled,
            Note = "Cancelled by admin"
        };
        var cancelRes = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{created.Id}/status", cancelDto);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await _adminClient.GetAsync($"api/admin/bookings/{created.Id}");
        var cancelled = await getRes.Content.ReadFromJsonAsync<BookingDto>();
        cancelled!.Status.Should().Be(BookingStatus.Cancelled);
        cancelled.AvailabilityBlockId.Should().BeNull();
    }

    [Fact]
    public async Task AdminUpdateStatus_InvalidTransition_ReturnsBadRequest()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser8");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2046, 4, 1),
            CheckOut = new DateOnly(2046, 4, 7),
            Guests = 2
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createRes.Content.ReadFromJsonAsync<BookingDto>();

        var statusDto = new UpdateBookingStatusDto { Status = BookingStatus.Completed };
        var res = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{created!.Id}/status", statusDto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UserCreate_AnonymousUser_ReturnsUnauthorized()
    {
        var anonClient = _factory.CreateClient();

        var dto = new CreateBookingDto
        {
            HouseId = Guid.NewGuid(),
            CheckIn = new DateOnly(2040, 1, 1),
            CheckOut = new DateOnly(2040, 1, 5),
            Guests = 2
        };

        var res = await anonClient.PostAsJsonAsync("api/bookings", dto);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminGet_NonExistentBooking_ReturnsNotFound()
    {
        var res = await _adminClient.GetAsync($"api/admin/bookings/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminList_FilterByStatus_ReturnsFiltered()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("bookuser10");

        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = new DateOnly(2047, 9, 1),
            CheckOut = new DateOnly(2047, 9, 5),
            Guests = 2
        };
        var createRes = await userClient.PostAsJsonAsync("api/bookings", dto);
        createRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var res = await _adminClient.GetAsync("api/admin/bookings?status=0");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var bookings = await res.Content.ReadFromJsonAsync<List<BookingListItemDto>>();
        bookings.Should().NotBeNull();
        bookings!.Should().OnlyContain(b => b.Status == BookingStatus.Pending);
    }

    [Fact]
    public async Task AdminUpdateStatus_ConfirmOverlappingBooking_ReturnsConflict()
    {
        // Two guests book overlapping dates while both are still pending: neither holds an
        // availability block yet, so both requests are accepted. Confirming the second one
        // must be refused, otherwise the house is double-booked.
        var houseId = await GetPublishedHouseIdAsync();
        var first = await CreatePendingBookingAsync("overlap-admin-1", houseId, new DateOnly(2050, 7, 1), new DateOnly(2050, 7, 8));
        var second = await CreatePendingBookingAsync("overlap-admin-2", houseId, new DateOnly(2050, 7, 5), new DateOnly(2050, 7, 12));

        var confirmFirst = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{first.Id}/status",
            new UpdateBookingStatusDto { Status = BookingStatus.Confirmed });
        confirmFirst.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var confirmSecond = await _adminClient.PutAsJsonAsync($"api/admin/bookings/{second.Id}/status",
            new UpdateBookingStatusDto { Status = BookingStatus.Confirmed });
        confirmSecond.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var getRes = await _adminClient.GetAsync($"api/admin/bookings/{second.Id}");
        var stillPending = await getRes.Content.ReadFromJsonAsync<BookingDto>();
        stillPending!.Status.Should().Be(BookingStatus.Pending);
        stillPending.AvailabilityBlockId.Should().BeNull();
    }

    [Fact]
    public async Task OwnerConfirm_OverlappingBooking_ReturnsConflict()
    {
        // Same scenario through the owner's own confirm endpoint, which has its own code path.
        var houseId = await GetPublishedHouseIdAsync();
        var ownerClient = _factory.CreateOwnerClient("overlap-owner");
        var ownerId = ReadUserId(ownerClient);

        var assign = await _adminClient.PutAsJsonAsync($"api/admin/houses/{houseId}/owner",
            new AssignOwnerRequest { OwnerId = ownerId });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var first = await CreatePendingBookingAsync("overlap-guest-1", houseId, new DateOnly(2051, 3, 1), new DateOnly(2051, 3, 8));
        var second = await CreatePendingBookingAsync("overlap-guest-2", houseId, new DateOnly(2051, 3, 4), new DateOnly(2051, 3, 10));

        var confirmFirst = await ownerClient.PostAsJsonAsync($"api/owner/bookings/{first.Id}/confirm", new { note = "ok" });
        confirmFirst.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var confirmSecond = await ownerClient.PostAsJsonAsync($"api/owner/bookings/{second.Id}/confirm", new { note = "ok" });
        confirmSecond.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var getRes = await _adminClient.GetAsync($"api/admin/bookings/{second.Id}");
        var stillPending = await getRes.Content.ReadFromJsonAsync<BookingDto>();
        stillPending!.Status.Should().Be(BookingStatus.Pending);
    }

    private async Task<BookingDto> CreatePendingBookingAsync(string username, Guid houseId, DateOnly checkIn, DateOnly checkOut)
    {
        var userClient = _factory.CreateUserClient(username);
        var res = await userClient.PostAsJsonAsync("api/bookings", new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = 2
        });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var booking = await res.Content.ReadFromJsonAsync<BookingDto>();
        booking!.Status.Should().Be(BookingStatus.Pending);
        return booking;
    }

    private static string ReadUserId(HttpClient client)
    {
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub").Value;
    }

    private async Task<Guid> GetPublishedHouseIdAsync()
    {
        var houses = await _adminClient.GetFromJsonAsync<PageResult<AdminHouseListItemDto>>("api/admin/houses?pageSize=1");
        var houseId = houses!.Items.First().Id;

        // Directly set Published in DB (bypasses lifecycle image requirement for tests)
        _factory.EnsureHousePublished(houseId);

        return houseId;
    }
}
