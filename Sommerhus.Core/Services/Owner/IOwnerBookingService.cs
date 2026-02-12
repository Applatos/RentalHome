using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Owner;

public interface IOwnerBookingService
{
    Task<IReadOnlyList<BookingListItemDto>> ListByOwnerAsync(string ownerId, CancellationToken ct);
    Task<ServiceResult<BookingDto>> GetAsync(string ownerId, Guid bookingId, CancellationToken ct);
    Task<ServiceResult> ConfirmAsync(string ownerId, Guid bookingId, string? ownerNote, CancellationToken ct);
    Task<ServiceResult> RejectAsync(string ownerId, Guid bookingId, string? ownerNote, CancellationToken ct);
}
