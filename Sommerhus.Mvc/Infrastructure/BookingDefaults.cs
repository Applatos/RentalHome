using System.Globalization;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// The stay a house page and a booking form start from, and the limits both apply, so that the
/// selection carried from one to the other means the same thing on both.
/// </summary>
public static class BookingDefaults
{
    /// <summary>The upper guest bound used when the house's capacity is unknown.</summary>
    public const int FallbackMaxGuests = 20;

    /// <summary>The number of guests the price includes, and so the default.</summary>
    public const int IncludedGuests = 2;

    private const int DaysAhead = 7;
    private const int Nights = 7;

    /// <summary>Today as the API sees it when it rejects a check-in in the past.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static DateOnly Arrival(DateOnly today) => today.AddDays(DaysAhead);

    public static DateOnly Departure(DateOnly arrival) => arrival.AddDays(Nights);

    public static int MaxGuests(int? capacity) => capacity is > 0 ? capacity.Value : FallbackMaxGuests;

    public static int Guests(int? capacity) => Math.Min(IncludedGuests, MaxGuests(capacity));

    /// <summary>A requested guest count kept within 1 and the capacity; the default when none was requested.</summary>
    public static int ClampGuests(int? requested, int? capacity)
        => requested is { } guests ? Math.Clamp(guests, 1, MaxGuests(capacity)) : Guests(capacity);

    /// <summary>The date format of date inputs and of the booking link's query.</summary>
    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>The booking form for a house, with the stay filled in.</summary>
    public static string BookUrl(Guid houseId, DateOnly checkIn, DateOnly checkOut, int guests)
        => $"/houses/{houseId}/book?checkIn={FormatDate(checkIn)}&checkOut={FormatDate(checkOut)}&guests={guests.ToString(CultureInfo.InvariantCulture)}";
}
