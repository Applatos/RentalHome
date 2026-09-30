using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Houses;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Houses;

public sealed class AdminHouseService(
    AppDbContext db,
    IImageStorage imageStorage,
    ISearchIndexer searchIndexer,
    IPriceSummaryService priceSummaryService) : IAdminHouseService
{

    public async Task<PageResult<AdminHouseListItemDto>> SearchAsync(string? query, EntityStatus? status, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var houseQuery = db.Houses.AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Areas)
            .AsQueryable();

        if (status.HasValue)
        {
            houseQuery = houseQuery.Where(h => h.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            houseQuery = houseQuery.Where(h =>
                h.Title != null && EF.Functions.Like(h.Title, $"%{term}%") ||
                h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%") ||
                h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%"));
        }

        var total = await houseQuery.CountAsync(ct);

        var items = await houseQuery
            .OrderByDescending(h => h.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new AdminHouseListItemDto(
                h.Id,
                h.Title,
                h.City != null ? h.City.Name : null,
                h.City != null ? h.City.Zip : null,
                h.Address,
                h.Description,
                h.City != null ? $"{h.City.Zip}  {h.City.Name}" : null,
                h.Areas.OrderBy(a => a.Name).Select(a => a.Name).ToList(),
                h.CreatedAtUtc,
                h.Status))
            .ToListAsync(ct);

        return new PageResult<AdminHouseListItemDto>
        {
            Query = query ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<ServiceResult<AdminHouseDetailsDto>> GetDetailsAsync(Guid id, string baseUrl, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.Areas)
            .Include(x => x.Group)
            .Include(x => x.HouseFeatures).ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (house is null)
        {
            return ServiceResult<AdminHouseDetailsDto>.NotFound();
        }

        // The form edits this plan even when it is inactive; saving it updates the same plan.
        var plan = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .ForEditing(house.Id)
            .FirstOrDefaultAsync(ct);

        var effectiveCalendarId = house.CalendarOverrideId ?? house.Group?.DefaultCalendarId;
        var calendarSegments = effectiveCalendarId.HasValue
            ? await db.SeasonSpans
                .AsNoTracking()
                .Where(s => s.CalendarId == effectiveCalendarId.Value)
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.EndDate)
                .Select(s => new SeasonSpanDto(s.Id, s.StartDate, s.EndDate, s.Code, null, null))
                .ToListAsync(ct)
            : new List<SeasonSpanDto>();

        var details = MapDetails(house, baseUrl, plan, calendarSegments);
        return ServiceResult<AdminHouseDetailsDto>.Success(details);
    }

    public async Task<ServiceResult<Guid>> CreateAsync(UpsertHouseDto dto, CancellationToken ct)
    {
        var areasResult = await ResolveAreasAsync(dto.AreaIds, ct);
        if (!areasResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(areasResult.Errors);
        }

        var groupError = await ValidateGroupAsync(dto.GroupId, ct);
        if (groupError is not null)
        {
            return ServiceResult<Guid>.FailureFrom(groupError);
        }

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Address = dto.Address,
            CityId = dto.CityId,
            Description = dto.Description,
            SearchKeywords = NormalizeSearchKeywords(dto.SearchKeywords),
            GroupId = dto.GroupId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var area in areasResult.Value ?? Array.Empty<Area>())
        {
            house.Areas.Add(area);
        }

        db.Houses.Add(house);
        await db.SaveChangesAsync(ct);
        await searchIndexer.UpdateHouseAsync(house.Id, ct);

        return ServiceResult<Guid>.Success(house.Id);
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(x => x.Areas)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (house is null)
        {
            return ServiceResult.NotFound();
        }

        var areasResult = await ResolveAreasAsync(dto.AreaIds, ct);
        if (!areasResult.IsSuccess)
        {
            return ServiceResult.Invalid(areasResult.Errors);
        }

        var groupError = await ValidateGroupAsync(dto.GroupId, ct);
        if (groupError is not null)
        {
            return groupError;
        }

        // Without an override the group decides the house's calendar, so a change can alter its prices.
        var groupChanged = house.GroupId != dto.GroupId;

        house.Title = dto.Title;
        house.Address = dto.Address;
        house.CityId = dto.CityId;
        house.Description = dto.Description;
        house.SearchKeywords = NormalizeSearchKeywords(dto.SearchKeywords);
        house.GroupId = dto.GroupId;

        house.Areas.Clear();
        foreach (var area in areasResult.Value ?? Array.Empty<Area>())
        {
            house.Areas.Add(area);
        }

        await db.SaveChangesAsync(ct);

        if (groupChanged)
        {
            // Also refreshes the search document.
            await priceSummaryService.RecomputeSummaryAsync(id, ct);
        }
        else
        {
            await searchIndexer.UpdateHouseAsync(id, ct);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var house = await db.Houses.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (house is null)
        {
            return ServiceResult.NotFound();
        }

        db.Houses.Remove(house);
        await db.SaveChangesAsync(ct);
        await searchIndexer.RemoveHouseAsync(id, ct);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult<IReadOnlyList<Area>>> ResolveAreasAsync(IEnumerable<Guid>? areaIds, CancellationToken ct)
    {
        if (areaIds is null)
        {
            return ServiceResult<IReadOnlyList<Area>>.Success(Array.Empty<Area>());
        }

        var ids = areaIds.ToList();
        if (ids.Any(id => id == Guid.Empty))
        {
            return InvalidAreaIds();
        }

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return ServiceResult<IReadOnlyList<Area>>.Success(Array.Empty<Area>());
        }

        var areas = await db.Areas.Where(a => normalizedIds.Contains(a.Id)).ToListAsync(ct);
        if (areas.Count != normalizedIds.Count)
        {
            return InvalidAreaIds();
        }

        return ServiceResult<IReadOnlyList<Area>>.Success(areas);
    }

    private async Task<ServiceResult?> ValidateGroupAsync(Guid? groupId, CancellationToken ct)
    {
        if (groupId is null)
        {
            return null;
        }

        var exists = await db.HouseGroups.AsNoTracking().AnyAsync(g => g.Id == groupId.Value, ct);
        return exists
            ? null
            : ServiceResult.Invalid("groupId", "Unknown house group.");
    }

    private static string? NormalizeSearchKeywords(string? keywords)
    {
        var trimmed = (keywords ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static ServiceResult<IReadOnlyList<Area>> InvalidAreaIds()
        => ServiceResult<IReadOnlyList<Area>>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(UpsertHouseDto.AreaIds)] = new[] { "Unknown area" }
        });

    private AdminHouseDetailsDto MapDetails(VacationHouse house, string baseUrl, PricePlan? plan, IReadOnlyList<SeasonSpanDto> calendar)
    {
        var images = house.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .Select(i => new ImageDto(
                i.Id,
                imageStorage.GetUrl(baseUrl, ImageCategory.House, i.HouseId, i.FileName),
                i.Alt,
                i.Kind.ToString()))
            .ToList();

        var features = house.HouseFeatures
            .Select(hf =>
            {
                var f = hf.Feature;
                return new FeatureValueDto(
                    hf.FeatureId,
                    f?.Name ?? string.Empty,
                    f?.ValueType ?? FeatureValueType.Text,
                    f?.Unit,
                    imageStorage.GetUrl(baseUrl, f?.IconUrl),
                    hf.RawValue);
            })
            .ToList();

        var areaItems = house.Areas
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToList();

        var areaIds = areaItems.Select(a => a.Id).ToList();

        var planDto = plan is null ? null : PricePlanMapper.ToDto(plan);

        var calendarSource = house.CalendarOverrideId.HasValue
            ? "Custom (house override)"
            : house.Group?.DefaultCalendarId.HasValue == true
                ? $"Group: {house.Group.Name}"
                : null;

        return new AdminHouseDetailsDto(
            house.Id,
            house.Title,
            house.City != null ? house.City.Name : null,
            house.City != null ? house.City.Zip : null,
            house.Address,
            house.Description,
            images,
            features,
            house.CityId,
            house.City != null ? $"{house.City.Zip}  {house.City.Name}" : null,
            areaIds,
            areaItems,
            house.CreatedAtUtc,
            calendar,
            planDto,
            house.GroupId,
            house.Status,
            house.SearchKeywords,
            house.PublishedAtUtc,
            house.ArchivedAtUtc,
            house.CalendarOverrideId,
            calendarSource);
    }

}
