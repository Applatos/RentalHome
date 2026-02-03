using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Repository.Admin.HouseGroups;

public sealed class AdminHouseGroupService : IAdminHouseGroupService
{
    private readonly AppDbContext _db;

    public AdminHouseGroupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct)
        => await _db.HouseGroups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new LookupItem(g.Id, g.Name))
            .ToListAsync(ct);

    public async Task<ServiceResult<IReadOnlyList<HouseGroupListItemDto>>> ListAsync(CancellationToken ct)
    {
        var groups = await _db.HouseGroups
            .AsNoTracking()
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct);

        var houseCounts = await _db.Houses
            .AsNoTracking()
            .Where(h => h.GroupId.HasValue)
            .GroupBy(h => h.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.GroupId, g => g.Count, ct);

        var result = groups.Select(g => new HouseGroupListItemDto(
            g.Id,
            g.Name,
            houseCounts.TryGetValue(g.Id, out var count) ? count : 0
        )).OrderBy(g => g.Name).ToList();

        return ServiceResult<IReadOnlyList<HouseGroupListItemDto>>.Success(result);
    }

    public async Task<ServiceResult<HouseGroupDetailsDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var group = await _db.HouseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null)
            return ServiceResult<HouseGroupDetailsDto>.NotFound();

        var calendar = await GetCalendarAsync(id, ct);
        return ServiceResult<HouseGroupDetailsDto>.Success(new HouseGroupDetailsDto(group.Id, group.Name, calendar));
    }

    public async Task<ServiceResult<LookupItem>> CreateAsync(HouseGroupDto dto, CancellationToken ct)
    {
        var nameResult = NormalizeName(dto?.name);
        if (!nameResult.IsSuccess)
            return ServiceResult<LookupItem>.Invalid(CloneErrors(nameResult.Errors));

        var normalizedName = nameResult.Value!;
        var exists = await _db.HouseGroups.AsNoTracking().AnyAsync(g => g.Name == normalizedName, ct);
        if (exists)
            return ServiceResult<LookupItem>.Conflict(nameof(HouseGroupDto.name), "A group with this name already exists.");

        var entity = new HouseGroup
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
        };

        await _db.HouseGroups.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<LookupItem>.Success(new LookupItem(entity.Id, entity.Name));
    }

    public async Task<ServiceResult<HouseGroupDetailsDto>> UpdateAsync(Guid id, UpsertHouseGroupDto dto, CancellationToken ct)
    {
        var group = await _db.HouseGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null)
            return ServiceResult<HouseGroupDetailsDto>.NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult<HouseGroupDetailsDto>.Invalid("name", "Name is required.");

        group.Name = dto.Name.Trim();
        await _db.SaveChangesAsync(ct);

        var calendar = await GetCalendarAsync(id, ct);
        return ServiceResult<HouseGroupDetailsDto>.Success(new HouseGroupDetailsDto(group.Id, group.Name, calendar));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var group = await _db.HouseGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null)
            return ServiceResult.NotFound();

        var hasHouses = await _db.Houses.AnyAsync(h => h.GroupId == id, ct);
        if (hasHouses)
            return ServiceResult.Invalid("group", "Cannot delete group with assigned houses.");

        var spans = await _db.SeasonSpans.Where(s => s.GroupId == id).ToListAsync(ct);
        _db.SeasonSpans.RemoveRange(spans);
        _db.HouseGroups.Remove(group);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<SeasonSpanDto>> AddSeasonSpanAsync(Guid groupId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var groupExists = await _db.HouseGroups.AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var validationError = ValidateSeasonSpan(dto);
        if (validationError is not null)
            return validationError;

        var seasonCode = await _db.SeasonCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == dto.Code, ct);
        if (seasonCode is null)
            return ServiceResult<SeasonSpanDto>.Invalid("code", $"Season code '{dto.Code}' does not exist.");

        var span = new SeasonSpan
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Code = dto.Code,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        _db.SeasonSpans.Add(span);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<SeasonSpanDto>.Success(
            new SeasonSpanDto(span.Id, span.StartDate, span.EndDate, span.Code, seasonCode.Name, seasonCode.Color)
        );
    }

    public async Task<ServiceResult<SeasonSpanDto>> UpdateSeasonSpanAsync(Guid groupId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var span = await _db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.GroupId == groupId, ct);
        if (span is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        var validationError = ValidateSeasonSpan(dto);
        if (validationError is not null)
            return validationError;

        var seasonCode = await _db.SeasonCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code == dto.Code, ct);
        if (seasonCode is null)
            return ServiceResult<SeasonSpanDto>.Invalid("code", $"Season code '{dto.Code}' does not exist.");

        span.Code = dto.Code;
        span.StartDate = dto.StartDate;
        span.EndDate = dto.EndDate;
        await _db.SaveChangesAsync(ct);

        return ServiceResult<SeasonSpanDto>.Success(
            new SeasonSpanDto(span.Id, span.StartDate, span.EndDate, span.Code, seasonCode.Name, seasonCode.Color)
        );
    }

    public async Task<ServiceResult> DeleteSeasonSpanAsync(Guid groupId, Guid spanId, CancellationToken ct)
    {
        var span = await _db.SeasonSpans.FirstOrDefaultAsync(s => s.Id == spanId && s.GroupId == groupId, ct);
        if (span is null)
            return ServiceResult.NotFound();

        _db.SeasonSpans.Remove(span);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    // House season span management (when house has a group)
    public async Task<ServiceResult<SeasonSpanDto>> AddHouseSeasonSpanAsync(Guid houseId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null || house.GroupId is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        return await AddSeasonSpanAsync(house.GroupId.Value, dto, ct);
    }

    public async Task<ServiceResult<SeasonSpanDto>> UpdateHouseSeasonSpanAsync(Guid houseId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null || house.GroupId is null)
            return ServiceResult<SeasonSpanDto>.NotFound();

        return await UpdateSeasonSpanAsync(house.GroupId.Value, spanId, dto, ct);
    }

    public async Task<ServiceResult> DeleteHouseSeasonSpanAsync(Guid houseId, Guid spanId, CancellationToken ct)
    {
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null || house.GroupId is null)
            return ServiceResult.NotFound();

        return await DeleteSeasonSpanAsync(house.GroupId.Value, spanId, ct);
    }

    private async Task<IReadOnlyList<SeasonSpanDto>> GetCalendarAsync(Guid groupId, CancellationToken ct)
    {
        var seasonCodes = await _db.SeasonCodes.AsNoTracking().ToDictionaryAsync(c => c.Code, ct);

        var spans = await _db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.StartDate)
            .ToListAsync(ct);

        return spans.Select(s =>
        {
            seasonCodes.TryGetValue(s.Code, out var sc);
            return new SeasonSpanDto(s.Id, s.StartDate, s.EndDate, s.Code, sc?.Name, sc?.Color);
        }).ToList();
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
                [nameof(HouseGroupDto.name)] = new[] { "Name is required." }
            });
        }

        return ServiceResult<string>.Success(trimmed);
    }

    private static Dictionary<string, string[]> CloneErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var copy = new Dictionary<string, string[]>(errors.Count, StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            copy[pair.Key] = pair.Value.ToArray();
        }

        return copy;
    }
}
