using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Owner;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Owner;

public sealed class OwnerHouseService(
    AppDbContext db,
    IOwnerAuthorizationService ownerAuth,
    IImageStorage imageStorage) : IOwnerHouseService
{
    private readonly AppDbContext db = db;
    private readonly IOwnerAuthorizationService ownerAuth = ownerAuth;
    private readonly IImageStorage imageStorage = imageStorage;

    public async Task<IReadOnlyList<OwnerHouseListItemDto>> ListAsync(string ownerId, CancellationToken ct)
    {
        return await db.Houses.AsNoTracking()
            .Where(h => h.OwnerId == ownerId)
            .Include(h => h.City)
            .Include(h => h.Images)
            .OrderBy(h => h.Title)
            .Select(h => new OwnerHouseListItemDto(
                h.Id,
                h.Title,
                h.Address,
                h.City.Name,
                h.Status,
                h.Images.Count,
                h.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<AdminHouseDetailsDto>> GetDetailsAsync(
        string ownerId, Guid houseId, string baseUrl, CancellationToken ct)
    {
        if (!await ownerAuth.IsOwnerAsync(ownerId, houseId, ct))
            return ServiceResult<AdminHouseDetailsDto>.NotFound();

        var house = await db.Houses
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.Areas)
            .Include(x => x.Group)
            .Include(x => x.HouseFeatures).ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == houseId, ct);

        if (house is null)
            return ServiceResult<AdminHouseDetailsDto>.NotFound();

        var plan = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .Where(p => p.HouseId == house.Id && p.IsActive)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.UpdatedAtUtc.HasValue ? p.UpdatedAtUtc.Value : p.CreatedAtUtc)
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

    public async Task<ServiceResult> UpdateAsync(
        string ownerId, Guid houseId, OwnerUpdateHouseDto dto, CancellationToken ct)
    {
        if (!await ownerAuth.IsOwnerAsync(ownerId, houseId, ct))
            return ServiceResult.NotFound();

        var house = await db.Houses.FirstOrDefaultAsync(x => x.Id == houseId, ct);
        if (house is null)
            return ServiceResult.NotFound();

        house.Description = dto.Description;
        house.SearchKeywords = dto.SearchKeywords;
        house.UpdatedAtUtc = DateTime.UtcNow;
        house.UpdatedBy = ownerId;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private AdminHouseDetailsDto MapDetails(
        VacationHouse house, string baseUrl,
        Sommerhus.Domain.Models.Pricing.PricePlan? plan,
        List<SeasonSpanDto> calendarSegments)
    {
        var images = house.Images
            .OrderBy(i => i.Kind)
            .Select(i => new ImageDto(i.Id, imageStorage.GetUrl(baseUrl, i.FileName), i.Alt, i.Kind.ToString()))
            .ToList();

        var features = house.HouseFeatures
            .Select(f => new FeatureValueDto(
                f.FeatureId, f.Feature!.Name, f.Feature.ValueType,
                f.Feature.Unit, imageStorage.GetUrl(baseUrl, f.Feature.IconUrl), f.RawValue))
            .ToList();

        PricePlanDetailsDto? pricing = null;
        if (plan is not null)
        {
            var seasonPrices = plan.SeasonPrices
                .Select(sp => new SeasonPriceDto(sp.Id, sp.PricePlanId, sp.Code, sp.NightlyPrice))
                .ToList();
            pricing = new PricePlanDetailsDto(
                plan.Id, plan.HouseId, plan.Name, plan.Currency, plan.IsActive,
                plan.CreatedAtUtc, plan.UpdatedAtUtc, seasonPrices);
        }

        var calendarSource = house.CalendarOverrideId.HasValue ? "Override"
            : house.Group?.DefaultCalendarId.HasValue == true ? "Group" : null;

        return new AdminHouseDetailsDto(
            house.Id,
            house.Title,
            house.City?.Name,
            house.City?.Zip,
            house.Address,
            house.Description,
            images,
            features,
            house.CityId,
            house.City is not null ? $"{house.City.Zip} {house.City.Name}" : null,
            house.Areas.Select(a => a.Id).ToList(),
            house.Areas.Select(a => new LookupItem(a.Id, a.Name)).ToList(),
            house.CreatedAtUtc,
            calendarSegments,
            pricing,
            house.GroupId,
            house.Status,
            house.SearchKeywords,
            house.PublishedAtUtc,
            house.ArchivedAtUtc,
            house.CalendarOverrideId,
            calendarSource);
    }
}
