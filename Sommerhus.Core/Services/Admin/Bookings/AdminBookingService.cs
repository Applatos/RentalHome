using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Bookings;

public sealed class AdminBookingService(AppDbContext db) : IAdminBookingService
{
    public async Task<IReadOnlyList<BookingListItemDto>> ListAsync(
        BookingFilterDto? filter, CancellationToken ct)
    {
        var query = db.Bookings.AsNoTracking().Include(b => b.House).AsQueryable();

        if (filter?.HouseId.HasValue == true)
            query = query.Where(b => b.HouseId == filter.HouseId.Value);

        if (!string.IsNullOrWhiteSpace(filter?.UserId))
            query = query.Where(b => b.UserId == filter.UserId);

        if (filter?.Status.HasValue == true)
            query = query.Where(b => b.Status == filter.Status.Value);

        return await query
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

    public async Task<ServiceResult<BookingDto>> GetAsync(Guid bookingId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            return ServiceResult<BookingDto>.NotFound();

        return ServiceResult<BookingDto>.Success(ToDto(booking));
    }

    public async Task<ServiceResult> UpdateStatusAsync(
        Guid bookingId, UpdateBookingStatusDto dto, string adminUserId, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.House)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            return ServiceResult.NotFound();

        var validTransition = IsValidTransition(booking.Status, dto.Status);
        if (!validTransition)
            return ServiceResult.Invalid("status",
                $"Cannot transition from {booking.Status} to {dto.Status}.");

        var previousStatus = booking.Status;
        booking.Status = dto.Status;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        booking.UpdatedBy = adminUserId;

        switch (dto.Status)
        {
            case BookingStatus.Confirmed:
                booking.ConfirmedAtUtc = DateTime.UtcNow;
                booking.OwnerNote = dto.Note?.Trim();
                await CreateAvailabilityBlockAsync(booking, adminUserId, ct);
                break;

            case BookingStatus.Cancelled:
                booking.CancelledAtUtc = DateTime.UtcNow;
                booking.CancelledBy = adminUserId;
                booking.OwnerNote = dto.Note?.Trim();
                await RemoveAvailabilityBlockAsync(booking, ct);
                break;

            case BookingStatus.Completed:
                break;
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private async Task CreateAvailabilityBlockAsync(Booking booking, string createdBy, CancellationToken ct)
    {
        var block = new AvailabilityBlock
        {
            HouseId = booking.HouseId,
            StartDate = booking.CheckIn,
            EndDate = booking.CheckOut,
            Status = AvailabilityStatus.Booked,
            Source = AvailabilitySource.Booking,
            Note = $"Booking {booking.Id}",
            CreatedBy = createdBy
        };

        db.AvailabilityBlocks.Add(block);
        await db.SaveChangesAsync(ct);

        booking.AvailabilityBlockId = block.Id;
    }

    private async Task RemoveAvailabilityBlockAsync(Booking booking, CancellationToken ct)
    {
        if (!booking.AvailabilityBlockId.HasValue)
            return;

        var block = await db.AvailabilityBlocks
            .FirstOrDefaultAsync(b => b.Id == booking.AvailabilityBlockId.Value, ct);

        if (block is not null)
            db.AvailabilityBlocks.Remove(block);

        booking.AvailabilityBlockId = null;
    }

    private static bool IsValidTransition(BookingStatus current, BookingStatus target) =>
        (current, target) switch
        {
            (BookingStatus.Pending, BookingStatus.Confirmed) => true,
            (BookingStatus.Pending, BookingStatus.Cancelled) => true,
            (BookingStatus.Confirmed, BookingStatus.Completed) => true,
            (BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
            _ => false
        };

    private static BookingDto ToDto(Booking b) =>
        new(b.Id, b.HouseId, b.House.Title, b.House.Address, b.UserId, null,
            b.CheckIn, b.CheckOut, b.Guests, b.Currency, b.TotalPrice,
            b.Status, b.GuestNote, b.OwnerNote, b.AvailabilityBlockId,
            b.ConfirmedAtUtc, b.CancelledAtUtc, b.CancelledBy, b.CreatedAtUtc);
}
