using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.HouseGroups;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.HouseGroups;

public sealed class AdminHouseGroupService : IAdminHouseGroupService
{
    private readonly AppDbContext db;

    public AdminHouseGroupService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct)
        => await db.HouseGroups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new LookupItem(g.Id, g.Name))
            .ToListAsync(ct);

    public async Task<ServiceResult<IReadOnlyList<HouseGroupDto>>> ListAsync(CancellationToken ct)
    {
        var groups = await db.HouseGroups
            .AsNoTracking()
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct);

        var houseCounts = await db.Houses
            .AsNoTracking()
            .Where(h => h.GroupId.HasValue)
            .GroupBy(h => h.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.GroupId, g => g.Count, ct);

        var result = groups.Select(g => new HouseGroupDto(
            g.Id,
            g.Name,
            houseCounts.TryGetValue(g.Id, out var count) ? count : 0
        )).OrderBy(g => g.Name).ToList();

        return ServiceResult<IReadOnlyList<HouseGroupDto>>.Success(result);
    }

    public async Task<ServiceResult<HouseGroupDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var group = await db.HouseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
            return ServiceResult<HouseGroupDto>.NotFound();

        var calendar = await GetCalendarAsync(id, ct);
        return ServiceResult<HouseGroupDto>.Success(new HouseGroupDto(group.Id, group.Name, Calendar: calendar));
    }

    public async Task<ServiceResult<LookupItem>> CreateAsync(HouseGroupDto dto, CancellationToken ct)
    {
        var nameResult = NormalizeName(dto?.Name);
        if (!nameResult.IsSuccess)
            return ServiceResult<LookupItem>.Invalid(nameResult.Errors);

        var normalizedName = nameResult.Value!;
        var exists = await db.HouseGroups.AsNoTracking().AnyAsync(g => g.Name == normalizedName, ct);
        if (exists)
            return ServiceResult<LookupItem>.Conflict(nameof(HouseGroupDto.Name), "A group with this name already exists.");

        var entity = new HouseGroup
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
        };

        await db.HouseGroups.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);

        return ServiceResult<LookupItem>.Success(new LookupItem(entity.Id, entity.Name));
    }

    public async Task<ServiceResult<HouseGroupDto>> UpdateAsync(Guid id, UpsertHouseGroupDto dto, CancellationToken ct)
    {
        var group = await db.HouseGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null)
            return ServiceResult<HouseGroupDto>.NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult<HouseGroupDto>.Invalid("name", "Name is required.");

        group.Name = dto.Name.Trim();
        await db.SaveChangesAsync(ct);

        var calendar = await GetCalendarAsync(id, ct);
        return ServiceResult<HouseGroupDto>.Success(new HouseGroupDto(group.Id, group.Name, Calendar: calendar));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var group = await db.HouseGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null)
            return ServiceResult.NotFound();

        var hasHouses = await db.Houses.AnyAsync(h => h.GroupId == id, ct);
        if (hasHouses)
            return ServiceResult.Invalid("group", "Cannot delete group with assigned houses.");

        db.HouseGroups.Remove(group);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<SeasonSpanDto>> AddSeasonSpanAsync(Guid groupId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var group = await db.HouseGroups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var calendarId = await EnsureGroupCalendarAsync(group, ct);
        return await AddSpanToCalendarAsync(calendarId, dto, ct);
    }

    public async Task<ServiceResult<SeasonSpanDto>> UpdateSeasonSpanAsync(Guid groupId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var group = await db.HouseGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group?.DefaultCalendarId is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var span = await db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.CalendarId == group.DefaultCalendarId, ct);
        if (span is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var validationError = ValidateSeasonSpan(dto);
        if (validationError is not null)
            return validationError;

        var seasonCode = await db.SeasonCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == dto.Code, ct);
        if (seasonCode is null)
            return ServiceResult<SeasonSpanDto>.Invalid("code", $"Season code '{dto.Code}' does not exist.");

        span.Code = dto.Code;
        span.StartDate = dto.StartDate;
        span.EndDate = dto.EndDate;
        await db.SaveChangesAsync(ct);

        return ServiceResult<SeasonSpanDto>.Success(
            new SeasonSpanDto(span.Id, span.StartDate, span.EndDate, span.Code, seasonCode.Name, seasonCode.Color)
        );
    }

    public async Task<ServiceResult> DeleteSeasonSpanAsync(Guid groupId, Guid spanId, CancellationToken ct)
    {
        var group = await db.HouseGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group?.DefaultCalendarId is null)
            return ServiceResult.NotFound();

        var span = await db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.CalendarId == group.DefaultCalendarId, ct);
        if (span is null)
            return ServiceResult.NotFound();

        db.SeasonSpans.Remove(span);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // House season span management — resolves effective calendar (override or group default)
    public async Task<ServiceResult<SeasonSpanDto>> AddHouseSeasonSpanAsync(Guid houseId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var calendarId = await ResolveEffectiveCalendarIdAsync(houseId, ct);
        if (calendarId is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        return await AddSpanToCalendarAsync(calendarId.Value, dto, ct);
    }

    public async Task<ServiceResult<SeasonSpanDto>> UpdateHouseSeasonSpanAsync(Guid houseId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var calendarId = await ResolveEffectiveCalendarIdAsync(houseId, ct);
        if (calendarId is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var span = await db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.CalendarId == calendarId, ct);
        if (span is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var validationError = ValidateSeasonSpan(dto);
        if (validationError is not null)
            return validationError;

        var seasonCode = await db.SeasonCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == dto.Code, ct);
        if (seasonCode is null)
            return ServiceResult<SeasonSpanDto>.Invalid("code", $"Season code '{dto.Code}' does not exist.");

        span.Code = dto.Code;
        span.StartDate = dto.StartDate;
        span.EndDate = dto.EndDate;
        await db.SaveChangesAsync(ct);

        return ServiceResult<SeasonSpanDto>.Success(
            new SeasonSpanDto(span.Id, span.StartDate, span.EndDate, span.Code, seasonCode.Name, seasonCode.Color)
        );
    }

    public async Task<ServiceResult> DeleteHouseSeasonSpanAsync(Guid houseId, Guid spanId, CancellationToken ct)
    {
        var calendarId = await ResolveEffectiveCalendarIdAsync(houseId, ct);
        if (calendarId is null)
            return ServiceResult.NotFound();

        var span = await db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.CalendarId == calendarId, ct);
        if (span is null)
            return ServiceResult.NotFound();

        db.SeasonSpans.Remove(span);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    private async Task<IReadOnlyList<SeasonSpanDto>> GetCalendarAsync(Guid groupId, CancellationToken ct)
    {
        var calendarId = await db.HouseGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => g.DefaultCalendarId)
            .FirstOrDefaultAsync(ct);

        if (calendarId is null)
            return [];

        return await GetSpansByCalendarIdAsync(calendarId.Value, ct);
    }

    private async Task<IReadOnlyList<SeasonSpanDto>> GetSpansByCalendarIdAsync(Guid calendarId, CancellationToken ct)
    {
        var seasonCodes = await db.SeasonCodes.AsNoTracking().ToDictionaryAsync(c => c.Code, ct);

        var spans = await db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.CalendarId == calendarId)
            .OrderBy(s => s.StartDate)
            .ToListAsync(ct);

        return spans.Select(s =>
        {
            seasonCodes.TryGetValue(s.Code, out var sc);
            return new SeasonSpanDto(s.Id, s.StartDate, s.EndDate, s.Code, sc?.Name, sc?.Color);
        }).ToList();
    }

    private async Task<Guid> EnsureGroupCalendarAsync(HouseGroup group, CancellationToken ct)
    {
        if (group.DefaultCalendarId.HasValue)
            return group.DefaultCalendarId.Value;

        var calendar = new SeasonCalendar
        {
            Name = $"{group.Name} Calendar",
            IsTemplate = false
        };
        db.SeasonCalendars.Add(calendar);
        group.DefaultCalendarId = calendar.Id;
        await db.SaveChangesAsync(ct);

        return calendar.Id;
    }

    private async Task<ServiceResult<SeasonSpanDto>> AddSpanToCalendarAsync(Guid calendarId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var validationError = ValidateSeasonSpan(dto);
        if (validationError is not null)
            return validationError;

        var seasonCode = await db.SeasonCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == dto.Code, ct);
        if (seasonCode is null)
            return ServiceResult<SeasonSpanDto>.Invalid("code", $"Season code '{dto.Code}' does not exist.");

        var span = new SeasonSpan
        {
            Id = Guid.NewGuid(),
            CalendarId = calendarId,
            Code = dto.Code,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        db.SeasonSpans.Add(span);
        await db.SaveChangesAsync(ct);

        return ServiceResult<SeasonSpanDto>.Success(
            new SeasonSpanDto(span.Id, span.StartDate, span.EndDate, span.Code, seasonCode.Name, seasonCode.Color)
        );
    }

    private async Task<Guid?> ResolveEffectiveCalendarIdAsync(Guid houseId, CancellationToken ct)
    {
        var house = await db.Houses
            .AsNoTracking()
            .Include(h => h.Group)
            .FirstOrDefaultAsync(h => h.Id == houseId, ct);

        if (house is null)
            return null;

        return house.CalendarOverrideId
            ?? house.Group?.DefaultCalendarId;
    }

    private static ServiceResult<SeasonSpanDto>? ValidateSeasonSpan(UpsertSeasonSpanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            return ServiceResult<SeasonSpanDto>.Invalid("code", "Season code is required.");

        if (dto.EndDate < dto.StartDate)
            return ServiceResult<SeasonSpanDto>.Invalid("endDate", "End date must be after start date.");

        return null;
    }

    private static ServiceResult<string> NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return ServiceResult<string>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(HouseGroupDto.Name)] = new[] { "Name is required." }
            });
        }

        return ServiceResult<string>.Success(trimmed);
    }

}
