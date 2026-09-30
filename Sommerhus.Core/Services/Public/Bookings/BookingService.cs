using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Bookings;

public sealed class BookingService(
    AppDbContext db,
    IPricingQuoteService pricingService) : IBookingService
{
    public async Task<ServiceResult<BookingDto>> CreateAsync(
        string userId, CreateBookingDto dto, CancellationToken ct)
    {
        if (dto.CheckOut <= dto.CheckIn)
            return ServiceResult<BookingDto>.Invalid("checkOut", "Check-out must be after check-in.");

        if (dto.CheckIn < DateOnly.FromDateTime(DateTime.UtcNow))
            return ServiceResult<BookingDto>.Invalid("checkIn", "Check-in date cannot be in the past.");

        var house = await db.Houses.AsNoTracking()
            .Include(h => h.City)
            .FirstOrDefaultAsync(h => h.Id == dto.HouseId, ct);

        if (house is null)
            return ServiceResult<BookingDto>.NotFound();

        if (house.Status != EntityStatus.Published)
            return ServiceResult<BookingDto>.Invalid("houseId", "House is not available for booking.");

        // The quote checks guests, availability and that every night is priced, in that order.
        // Its failure is passed on as is, so a booking is never stored with a partial price.
        var quoteRequest = new PriceQuoteRequestDto(dto.HouseId, dto.CheckIn, dto.CheckOut, dto.Guests, null);
        var quoteResult = await pricingService.QuoteAsync(quoteRequest, ct);

        if (!quoteResult.IsSuccess)
            return ServiceResult<BookingDto>.FailureFrom(quoteResult);

        if (quoteResult.Value is null)
            return ServiceResult<BookingDto>.Unavailable("Unable to calculate price for the selected dates.");

        var quote = quoteResult.Value;

        var booking = new Booking
        {
            HouseId = dto.HouseId,
            UserId = userId,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            Guests = dto.Guests,
            Currency = quote.Currency,
            TotalPrice = quote.Total,
            Status = BookingStatus.Pending,
            GuestNote = dto.GuestNote?.Trim(),
            CreatedBy = userId
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        return ServiceResult<BookingDto>.Success(ToDto(booking, house.Title, house.Address, null));
    }

    public async Task<IReadOnlyList<BookingListItemDto>> ListByUserAsync(
        string userId, CancellationToken ct)
    {
        return await db.Bookings.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.House)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookingListItemDto(
                b.Id,
                b.HouseId,
                b.House.Title,
                null,
                b.CheckIn,
                b.CheckOut,
                b.Guests,
                b.Currency,
                b.TotalPrice,
                b.Status,
                b.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<BookingDto>> GetAsync(
        string userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, ct);

        if (booking is null)
            return ServiceResult<BookingDto>.NotFound();

        return ServiceResult<BookingDto>.Success(
            ToDto(booking, booking.House.Title, booking.House.Address, null));
    }

    public async Task<ServiceResult> CancelAsync(
        string userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, ct);

        if (booking is null)
            return ServiceResult.NotFound();

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return ServiceResult.Invalid("status", "Booking cannot be cancelled in its current state.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        booking.CancelledBy = userId;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        booking.UpdatedBy = userId;

        if (booking.AvailabilityBlockId.HasValue)
        {
            var block = await db.AvailabilityBlocks
                .FirstOrDefaultAsync(b => b.Id == booking.AvailabilityBlockId.Value, ct);
            if (block is not null)
                db.AvailabilityBlocks.Remove(block);

            booking.AvailabilityBlockId = null;
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private static BookingDto ToDto(Booking b, string houseTitle, string? houseAddress, string? userName) =>
        new(b.Id, b.HouseId, houseTitle, houseAddress, b.UserId, userName,
            b.CheckIn, b.CheckOut, b.Guests, b.Currency, b.TotalPrice,
            b.Status, b.GuestNote, b.OwnerNote, b.AvailabilityBlockId,
            b.ConfirmedAtUtc, b.CancelledAtUtc, b.CancelledBy, b.CreatedAtUtc);
}
