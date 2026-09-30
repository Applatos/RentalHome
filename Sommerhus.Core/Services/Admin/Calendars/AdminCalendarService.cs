using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Calendars;

public sealed class AdminCalendarService(
    AppDbContext db,
    IPriceSummaryService priceSummaryService) : IAdminCalendarService
{
    public async Task<ServiceResult<IReadOnlyList<CalendarDto>>> ListAsync(CancellationToken ct)
    {
        var calendars = await db.SeasonCalendars
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CalendarDto(
                c.Id,
                c.Name,
                c.Year,
                c.IsTemplate,
                c.Spans.Count))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<CalendarDto>>.Success(calendars);
    }

    public async Task<ServiceResult<CalendarDto>> GetAsync(Guid calendarId, CancellationToken ct)
    {
        var calendar = await db.SeasonCalendars
            .AsNoTracking()
            .Where(c => c.Id == calendarId)
            .Select(c => new CalendarDto(
                c.Id,
                c.Name,
                c.Year,
                c.IsTemplate,
                c.Spans.Count))
            .FirstOrDefaultAsync(ct);

        return calendar is null
            ? ServiceResult<CalendarDto>.NotFound()
            : ServiceResult<CalendarDto>.Success(calendar);
    }

    public async Task<ServiceResult<CalendarDto>> CreateAsync(UpsertCalendarDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult<CalendarDto>.Invalid("name", "Name is required.");

        var entity = new SeasonCalendar
        {
            Name = dto.Name.Trim(),
            Year = dto.Year,
            IsTemplate = dto.IsTemplate
        };

        db.SeasonCalendars.Add(entity);
        await db.SaveChangesAsync(ct);

        return ServiceResult<CalendarDto>.Success(
            new CalendarDto(entity.Id, entity.Name, entity.Year, entity.IsTemplate, 0));
    }

    public async Task<ServiceResult<CalendarDto>> UpdateAsync(Guid calendarId, UpsertCalendarDto dto, CancellationToken ct)
    {
        var entity = await db.SeasonCalendars.FirstOrDefaultAsync(c => c.Id == calendarId, ct);
        if (entity is null)
            return ServiceResult<CalendarDto>.NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult<CalendarDto>.Invalid("name", "Name is required.");

        entity.Name = dto.Name.Trim();
        entity.Year = dto.Year;
        entity.IsTemplate = dto.IsTemplate;
        await db.SaveChangesAsync(ct);

        var spanCount = await db.SeasonSpans.CountAsync(s => s.CalendarId == calendarId, ct);
        return ServiceResult<CalendarDto>.Success(
            new CalendarDto(entity.Id, entity.Name, entity.Year, entity.IsTemplate, spanCount));
    }

    public async Task<ServiceResult> DeleteAsync(Guid calendarId, CancellationToken ct)
    {
        var entity = await db.SeasonCalendars.FirstOrDefaultAsync(c => c.Id == calendarId, ct);
        if (entity is null)
            return ServiceResult.NotFound();

        var usedByGroup = await db.HouseGroups.AnyAsync(g => g.DefaultCalendarId == calendarId, ct);
        var usedByHouse = await db.Houses.AnyAsync(h => h.CalendarOverrideId == calendarId, ct);
        if (usedByGroup || usedByHouse)
            return ServiceResult.Invalid("calendar", "Cannot delete a calendar that is in use by a group or house.");

        db.SeasonCalendars.Remove(entity);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetHouseCalendarOverrideAsync(Guid houseId, Guid calendarId, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null)
            return ServiceResult.NotFound();

        var calendarExists = await db.SeasonCalendars.AnyAsync(c => c.Id == calendarId, ct);
        if (!calendarExists)
            return ServiceResult.Invalid("calendarId", "Calendar not found.");

        house.CalendarOverrideId = calendarId;
        await db.SaveChangesAsync(ct);
        await priceSummaryService.RecomputeSummaryAsync(houseId, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveHouseCalendarOverrideAsync(Guid houseId, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null)
            return ServiceResult.NotFound();

        house.CalendarOverrideId = null;
        await db.SaveChangesAsync(ct);
        await priceSummaryService.RecomputeSummaryAsync(houseId, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<CalendarDto>> CreateHouseOverrideAsync(Guid houseId, string? name, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null)
            return ServiceResult<CalendarDto>.NotFound();

        var trimmed = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            trimmed = $"Custom calendar for house {house.Title}";

        var calendar = new SeasonCalendar
        {
            Name = trimmed,
            IsTemplate = false
        };

        db.SeasonCalendars.Add(calendar);
        house.CalendarOverrideId = calendar.Id;
        await db.SaveChangesAsync(ct);
        await priceSummaryService.RecomputeSummaryAsync(houseId, ct);

        return ServiceResult<CalendarDto>.Success(
            new CalendarDto(calendar.Id, calendar.Name, calendar.Year, calendar.IsTemplate, 0));
    }
}
