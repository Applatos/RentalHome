using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Owner;

public sealed class OwnerBookingService(AppDbContext db) : IOwnerBookingService
{
    public async Task<IReadOnlyList<BookingListItemDto>> ListByOwnerAsync(
        string ownerId, CancellationToken ct)
    {
        var ownerHouseIds = await db.Houses.AsNoTracking()
            .Where(h => h.OwnerId == ownerId)
            .Select(h => h.Id)
            .ToListAsync(ct);

        return await db.Bookings.AsNoTracking()
            .Where(b => ownerHouseIds.Contains(b.HouseId))
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
        string ownerId, Guid bookingId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingDto>.NotFound();

        if (booking.House.OwnerId != ownerId)
            return ServiceResult<BookingDto>.NotFound();

        return ServiceResult<BookingDto>.Success(
            ToDto(booking, booking.House.Title, booking.House.Address));
    }

    public async Task<ServiceResult> ConfirmAsync(
        string ownerId, Guid bookingId, string? ownerNote, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null || booking.House.OwnerId != ownerId)
            return ServiceResult.NotFound();

        if (booking.Status != BookingStatus.Pending)
            return ServiceResult.Invalid("status", "Only pending bookings can be confirmed.");

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAtUtc = DateTime.UtcNow;
        booking.OwnerNote = ownerNote?.Trim();
        booking.UpdatedAtUtc = DateTime.UtcNow;
        booking.UpdatedBy = ownerId;

        var block = new AvailabilityBlock
        {
            HouseId = booking.HouseId,
            StartDate = booking.CheckIn,
            EndDate = booking.CheckOut,
            Status = AvailabilityStatus.Booked,
            Source = AvailabilitySource.Booking,
            Note = $"Booking {booking.Id}",
            CreatedBy = ownerId
        };

        db.AvailabilityBlocks.Add(block);
        await db.SaveChangesAsync(ct);

        booking.AvailabilityBlockId = block.Id;
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectAsync(
        string ownerId, Guid bookingId, string? ownerNote, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null || booking.House.OwnerId != ownerId)
            return ServiceResult.NotFound();

        if (booking.Status != BookingStatus.Pending)
            return ServiceResult.Invalid("status", "Only pending bookings can be rejected.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAtUtc = DateTime.UtcNow;
        booking.CancelledBy = ownerId;
        booking.OwnerNote = ownerNote?.Trim();
        booking.UpdatedAtUtc = DateTime.UtcNow;
        booking.UpdatedBy = ownerId;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private static BookingDto ToDto(Booking b, string houseTitle, string? houseAddress) =>
        new(b.Id, b.HouseId, houseTitle, houseAddress, b.UserId, null,
            b.CheckIn, b.CheckOut, b.Guests, b.Currency, b.TotalPrice,
            b.Status, b.GuestNote, b.OwnerNote, b.AvailabilityBlockId,
            b.ConfirmedAtUtc, b.CancelledAtUtc, b.CancelledBy, b.CreatedAtUtc);
}
