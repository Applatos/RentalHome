using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Houses;

public sealed class HouseQueryService(
    AppDbContext db,
    IImageStorage storage,
    IHouseSearchService houseSearchService) : IHouseQueryService
{
    public async Task<PageResult<PublicHouseListItemDto>> SearchAsync(HouseSearchFilter filter, string baseUrl, CancellationToken ct)
    {
        var result = await houseSearchService.SearchAsync(filter, baseUrl, ct);
        var items = result.Items
            .Select(i => new PublicHouseListItemDto(
                i.Id,
                i.Title,
                i.City,
                i.Zip,
                i.Address,
                i.Description,
                i.Images,
                i.Features,
                i.MinNightlyPrice,
                i.Currency,
                i.CoverUrl,
                i.Summary,
                i.Gallery,
                i.Areas))
            .ToList();

        return new PageResult<PublicHouseListItemDto>
        {
            Query = result.Query,
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            Items = items
        };
    }

    public async Task<PublicHouseDetailsDto?> GetAsync(Guid id, string baseUrl, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.Areas)
            .Include(x => x.HouseFeatures).ThenInclude(v => v.Feature)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == EntityStatus.Published, ct);

        if (house is null)
        {
            return null;
        }

        var summary = await db.HousePriceSummaries.AsNoTracking().FirstOrDefaultAsync(s => s.HouseId == id, ct);

        var gallery = house.Images
            .InDisplayOrder()
            .Select(i => new ImageDto(i.Id, storage.GetUrl(baseUrl, ImageCategory.House, i.HouseId, i.FileName), i.Alt, i.Kind.ToString()))
            .ToArray();

        var features = house.HouseFeatures
            .Select(hf =>
            {
                var f = hf.Feature;
                var icon = storage.GetUrl(baseUrl, f?.IconUrl);
                var dto = new FeatureValueDto(
                    Id: hf.FeatureId,
                    Name: f is null ? string.Empty : LocalizationNameResolver.Resolve(f.Name, f.NameEn),
                    ValueType: f?.ValueType ?? FeatureValueType.Text,
                    Unit: f?.Unit,
                    IconUrl: icon,
                    RawValue: hf.RawValue,
                    Key: f?.Key,
                    Category: f?.Category
                );
                return (SortOrder: f?.SortOrder ?? int.MaxValue, Dto: dto);
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Dto.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Dto)
            .ToList();

        return new PublicHouseDetailsDto(
            house.Id,
            house.Title,
            house.City?.Name,
            house.City?.Zip,
            house.Address,
            house.Description,
            gallery,
            features,
            MaxGuests: HouseCapacity.MaxGuests(house.HouseFeatures),
            Areas: house.Areas
                .Where(a => a.Status == EntityStatus.Published)
                .OrderBy(a => a.Name)
                .Select(a => new LookupItem(a.Id, LocalizationNameResolver.Resolve(a.Name, a.NameEn)))
                .ToList(),
            MinNightlyPrice: summary?.MinNightlyPrice,
            Currency: summary?.Currency);
    }

}
