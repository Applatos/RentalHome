using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Bookings;

public interface IAdminBookingService
{
    Task<IReadOnlyList<BookingListItemDto>> ListAsync(BookingFilterDto? filter, CancellationToken ct);
    Task<ServiceResult<BookingDto>> GetAsync(Guid bookingId, CancellationToken ct);
    Task<ServiceResult> UpdateStatusAsync(Guid bookingId, UpdateBookingStatusDto dto, string adminUserId, CancellationToken ct);
}

public sealed class BookingFilterDto
{
    public Guid? HouseId { get; set; }
    public string? UserId { get; set; }
    public BookingStatus? Status { get; set; }
}
