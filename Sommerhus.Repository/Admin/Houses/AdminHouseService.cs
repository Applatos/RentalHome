using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.Houses;
using Sommerhus.Application.Common;
using Sommerhus.Application.Storage;
using Sommerhus.Domain.Models;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Models;

namespace Sommerhus.Repository.Admin.Houses;

public sealed class AdminHouseService : IAdminHouseService
{
    private readonly AppDbContext db;
    private readonly IImageStorage imageStorage;

    public AdminHouseService(AppDbContext db, IImageStorage imageStorage)
    {
        this.db = db;
        this.imageStorage = imageStorage;
    }

    public async Task<PageResult<HouseListItemDto>> SearchAsync(string? query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var houseQuery = db.Houses.AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Areas)
            .AsQueryable();

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
            .OrderByDescending(h => h.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HouseListItemDto(
                h.Id,
                h.Title,
                h.City != null ? $"{h.City.Zip}  {h.City.Name}" : string.Empty,
                h.Areas.OrderBy(a => a.Name).Select(a => a.Name).ToList(),
                null,
                h.CreatedUtc))
            .ToListAsync(ct);

        return new PageResult<HouseListItemDto>
        {
            Query = query ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<ServiceResult<HouseDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct)
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
            return ServiceResult<HouseDetailsDto>.NotFound();
        }

        var plan = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .Where(p => p.HouseId == house.Id && p.IsActive)
            .OrderByDescending(rp => rp.IsActive)
            .ThenByDescending(rp => rp.UpdatedUtc ?? rp.CreatedUtc)
            .FirstOrDefaultAsync(ct);

        var calendarSegments = await db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.GroupId == house.GroupId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .Select(s => new SeasonSpanDto(s.Id, s.GroupId, s.StartDate, s.EndDate, s.Code))
            .ToListAsync(ct);

        var details = MapDetails(house, request, plan, calendarSegments);
        return ServiceResult<HouseDetailsDto>.Success(details);
    }

    public async Task<ServiceResult<Guid>> CreateAsync(UpsertHouseDto dto, CancellationToken ct)
    {
        var areasResult = await ResolveAreasAsync(dto.AreaIds, ct);
        if (!areasResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(CloneErrors(areasResult.Errors));
        }

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = dto.Name,
            Address = dto.Address,
            CityId = dto.CityId,
            Description = dto.Description,
            CreatedUtc = DateTime.UtcNow
        };

        foreach (var area in areasResult.Value ?? Array.Empty<Area>())
        {
            house.Areas.Add(area);
        }

        db.Houses.Add(house);
        await db.SaveChangesAsync(ct);

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
            return ServiceResult.Invalid(CloneErrors(areasResult.Errors));
        }

        house.Title = dto.Name;
        house.Address = dto.Address;
        house.CityId = dto.CityId;
        house.Description = dto.Description;

        house.Areas.Clear();
        foreach (var area in areasResult.Value ?? Array.Empty<Area>())
        {
            house.Areas.Add(area);
        }

        await db.SaveChangesAsync(ct);
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
        return ServiceResult.Success();
    }

    public async Task<FeatureUpsertOutcome> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct)
    {
        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
        {
            return FeatureUpsertOutcome.NotFound;
        }

        var normalized = FeatureValueNormalizer.Normalize(houseId, values);

        var featureIds = normalized.Select(i => i.FeatureId).Distinct().ToList();
        if (featureIds.Count > 0)
        {
            var existingFeatureIds = await db.Features
                .AsNoTracking()
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(ct);

            var missing = featureIds.Except(existingFeatureIds).ToList();
            if (missing.Count > 0)
            {
                return new FeatureUpsertOutcome(true, missing);
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.HouseFeatures.Where(hf => hf.HouseId == houseId).ExecuteDeleteAsync(ct);

        if (normalized.Count > 0)
        {
            await db.HouseFeatures.AddRangeAsync(normalized, ct);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return FeatureUpsertOutcome.Success;
    }

    public async Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
    {
        var house = await db.Houses.AsNoTracking().Include(h => h.Group).FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null)
        {
            return ServiceResult<PricePlanDetailsDto>.NotFound();
        }

        PricePlan? plan = null;
        if (dto.planId != Guid.Empty)
        {
            plan = await db.PricePlans
                .Include(p => p.SeasonPrices)
                .FirstOrDefaultAsync(p => p.HouseId == houseId && p.Id == dto.planId, ct);
        }

        plan ??= new PricePlan
        {
            HouseId = houseId
        };

        plan.Name = dto.Name;
        plan.Currency = dto.Currency;
        plan.IsActive = dto.IsActive;
        plan.UpdatedUtc = DateTime.UtcNow;

        if (db.Entry(plan).State == EntityState.Detached)
        {
            db.PricePlans.Add(plan);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.SeasonPrices
            .Where(s => s.PricePlanId == plan.Id)
            .ExecuteDeleteAsync(ct);

        if (dto.SeasonPrices.Count > 0)
        {
            var entities = dto.SeasonPrices.Select(s => new SeasonPrice
            {
                Id = Guid.NewGuid(),
                PricePlanId = plan.Id,
                Code = s.Code,
                NightlyPrice = s.NightlyPrice,
            }).ToList();

            await db.SeasonPrices.AddRangeAsync(entities, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var refreshed = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .FirstAsync(p => p.Id == plan.Id, ct);

        return ServiceResult<PricePlanDetailsDto>.Success(MapPlan(refreshed));
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

    private static ServiceResult<IReadOnlyList<Area>> InvalidAreaIds()
        => ServiceResult<IReadOnlyList<Area>>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(UpsertHouseDto.AreaIds)] = new[] { "Ukendt område" }
        });

    private HouseDetailsDto MapDetails(VacationHouse house, HttpRequest request, PricePlan? plan, IReadOnlyList<SeasonSpanDto> calendar)
    {
        var images = house.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .Select(i => new ImageDto(
                i.Id,
                imageStorage.GetUrl(request, ImageCategory.House, i.HouseId, i.FileName),
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
                    f?.ValueType.ToString() ?? string.Empty,
                    f?.Unit,
                    imageStorage.GetUrl(request, f?.IconUrl),
                    hf.RawValue);
            })
            .ToList();

        var areaItems = house.Areas
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToList();

        var areaIds = areaItems.Select(a => a.Id).ToList();

        var planDto = plan is null ? null : MapPlan(plan);

        return new HouseDetailsDto(
            house.Id,
            house.Title,
            house.CityId,
            house.City != null ? $"{house.City.Zip}  {house.City.Name}" : string.Empty,
            areaIds,
            areaItems,
            house.Address,
            house.Description,
            house.CreatedUtc,
            features,
            images,
            calendar,
            planDto);
    }

    private static PricePlanDetailsDto MapPlan(PricePlan plan)
    {
        var rates = plan.SeasonPrices
             .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
             .Select(s => new SeasonPriceDto(
                s.Id,
                s.PricePlanId,
                s.Code,
                s.NightlyPrice))
            .ToList();

        return new PricePlanDetailsDto(
            plan.Id,
            plan.HouseId,
            plan.Name,
            plan.Currency,
            plan.IsActive,
            plan.CreatedUtc,
            plan.UpdatedUtc,
            rates);
    }

    private static Dictionary<string, string[]> CloneErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            dict[pair.Key] = pair.Value?.ToArray() ?? Array.Empty<string>();
        }

        return dict;
    }
}