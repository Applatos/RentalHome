using FluentAssertions;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class BookingDefaultsTests
{
    [Theory]
    [InlineData(6, 6)]
    [InlineData(null, BookingDefaults.FallbackMaxGuests)]
    [InlineData(0, BookingDefaults.FallbackMaxGuests)]
    public void MaxGuests_IsTheCapacityWhenKnown(int? capacity, int expected)
        => BookingDefaults.MaxGuests(capacity).Should().Be(expected);

    [Theory]
    [InlineData(6, 2)]
    [InlineData(null, 2)]
    [InlineData(1, 1)]
    public void Guests_DefaultsToTheIncludedGuestsWithinTheCapacity(int? capacity, int expected)
        => BookingDefaults.Guests(capacity).Should().Be(expected);

    [Theory]
    [InlineData(4, 6, 4)]
    [InlineData(9, 6, 6)]
    [InlineData(0, 6, 1)]
    [InlineData(null, 6, 2)]
    [InlineData(30, null, BookingDefaults.FallbackMaxGuests)]
    public void ClampGuests_KeepsTheRequestWithinOneAndTheCapacity(int? requested, int? capacity, int expected)
        => BookingDefaults.ClampGuests(requested, capacity).Should().Be(expected);

    [Fact]
    public void BookUrl_CarriesTheStayInTheQueryTheBookingFormReads()
    {
        var houseId = new Guid("5fb7097c-335c-4d07-b4fd-000004e2d28c");

        BookingDefaults.BookUrl(houseId, new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 14), 2)
            .Should().Be("/houses/5fb7097c-335c-4d07-b4fd-000004e2d28c/book?checkIn=2026-10-07&checkOut=2026-10-14&guests=2");
    }

    [Fact]
    public void DefaultStay_StartsAWeekAheadAndLastsAWeek()
    {
        var today = new DateOnly(2026, 9, 30);
        var arrival = BookingDefaults.Arrival(today);

        arrival.Should().Be(new DateOnly(2026, 10, 7));
        BookingDefaults.Departure(arrival).Should().Be(new DateOnly(2026, 10, 14));
    }
}
