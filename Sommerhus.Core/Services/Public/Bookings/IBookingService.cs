using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Bookings;

public interface IBookingService
{
    Task<ServiceResult<BookingDto>> CreateAsync(string userId, CreateBookingDto dto, CancellationToken ct);
    Task<IReadOnlyList<BookingListItemDto>> ListByUserAsync(string userId, CancellationToken ct);
    Task<ServiceResult<BookingDto>> GetAsync(string userId, Guid bookingId, CancellationToken ct);
    Task<ServiceResult> CancelAsync(string userId, Guid bookingId, CancellationToken ct);
}
