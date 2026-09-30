using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Public;

public sealed class BookingPricingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly PricingScenario scenario;

    public BookingPricingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task Create_PricedStay_StoresTheQuotedTotal()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var user = factory.CreateUserClient("pricing-booker-1");

        using var res = await user.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 7, 6), 7, guests: 4));

        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        var booking = await res.Content.ReadFromJsonAsync<BookingDto>();
        booking!.TotalPrice.Should().Be(7 * HighRate + 2 * GuestFeePerNight * 7 + CleaningFee);
        booking.Currency.Should().Be("DKK");
    }

    [Fact]
    public async Task Create_StayWithUnpricedNight_ReturnsUnpricedNightsAndStoresNothing()
    {
        var groupId = await scenario.CreateGroupAsync();
        await scenario.AddGroupSpanAsync(groupId, "A", new DateOnly(Year, 7, 1), new DateOnly(Year, 7, 10));
        var houseId = await scenario.CreateHouseAsync(groupId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate });
        var user = factory.CreateUserClient("pricing-booker-2");

        // Jul 11 has no season.
        using var res = await user.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 7, 8), 4, guests: 2));

        (await ReadValidationErrorsAsync(res, PricingErrors.UnpricedNights)).Single().Should().Contain($"{Year}-07-11");
        (await CountBookingsAsync(houseId)).Should().Be(0);
    }

    [Fact]
    public async Task Create_MoreGuestsThanCapacity_ReturnsGuestsErrorAndStoresNothing()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var user = factory.CreateUserClient("pricing-booker-3");

        using var res = await user.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 7, 6), 3, guests: 8));

        (await ReadValidationErrorsAsync(res, PricingErrors.Guests)).Single().Should().Contain("6 guests");
        (await CountBookingsAsync(houseId)).Should().Be(0);
    }

    [Fact]
    public async Task Create_StayLongerThanAYear_ReturnsDatesError()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var user = factory.CreateUserClient("pricing-booker-4");

        using var res = await user.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 1, 1), 400, guests: 2));

        await ReadValidationErrorsAsync(res, PricingErrors.Dates);
        (await CountBookingsAsync(houseId)).Should().Be(0);
    }

    [Fact]
    public async Task Create_BlockedDates_StillReturnsConflict()
    {
        var (_, houseId) = await scenario.CreatePricedHouseAsync();
        var firstGuest = factory.CreateUserClient("pricing-booker-5");
        using (var first = await firstGuest.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 8, 3), 5, guests: 2)))
        {
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var booking = await first.Content.ReadFromJsonAsync<BookingDto>();
            using var confirm = await scenario.Admin.PutAsJsonAsync($"api/admin/bookings/{booking!.Id}/status",
                new UpdateBookingStatusDto { Status = Sommerhus.Domain.Models.BookingStatus.Confirmed });
            confirm.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var user = factory.CreateUserClient("pricing-booker-6");
        using var res = await user.PostAsJsonAsync("api/bookings", Booking(houseId, new DateOnly(Year, 8, 5), 2, guests: 2));

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static CreateBookingDto Booking(Guid houseId, DateOnly checkIn, int nights, int guests) => new()
    {
        HouseId = houseId,
        CheckIn = checkIn,
        CheckOut = checkIn.AddDays(nights),
        Guests = guests
    };

    private async Task<int> CountBookingsAsync(Guid houseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Bookings.CountAsync(b => b.HouseId == houseId);
    }
}
