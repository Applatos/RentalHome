using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Calendars;

public interface IAdminCalendarService
{
    Task<ServiceResult<IReadOnlyList<CalendarDto>>> ListAsync(CancellationToken ct);
    Task<ServiceResult<CalendarDto>> GetAsync(Guid calendarId, CancellationToken ct);
    Task<ServiceResult<CalendarDto>> CreateAsync(UpsertCalendarDto dto, CancellationToken ct);
    Task<ServiceResult<CalendarDto>> UpdateAsync(Guid calendarId, UpsertCalendarDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid calendarId, CancellationToken ct);

    Task<ServiceResult> SetHouseCalendarOverrideAsync(Guid houseId, Guid calendarId, CancellationToken ct);
    Task<ServiceResult> RemoveHouseCalendarOverrideAsync(Guid houseId, CancellationToken ct);
    Task<ServiceResult<CalendarDto>> CreateHouseOverrideAsync(Guid houseId, string name, CancellationToken ct);
}
