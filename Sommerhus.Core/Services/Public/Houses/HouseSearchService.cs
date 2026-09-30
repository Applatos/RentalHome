using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Houses;

public sealed class HouseSearchService(
    AppDbContext db,
    IImageStorage imageStorage,
    ISearchIndexer searchIndexer) : IHouseSearchService
{
    public async Task<PageResult<HouseSearchResultDto>> SearchAsync(HouseSearchFilter filter, string baseUrl, CancellationToken ct)
    {
        var normalized = Normalize(filter);

        var query = db.HouseSearchDocuments
            .AsNoTracking()
            .Where(d => d.Status == EntityStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalized.Query))
        {
            var term = normalized.Query.Trim();
            query = query.Where(d =>
                EF.Functions.Like(d.Title, $"%{term}%") ||
                EF.Functions.Like(d.SearchVector, $"%{term}%") ||
                (d.CityName != null && EF.Functions.Like(d.CityName, $"%{term}%")) ||
                (d.CityZip != null && EF.Functions.Like(d.CityZip, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(normalized.City))
        {
            var city = normalized.City.Trim();
            query = query.Where(d =>
                (d.CityName != null && EF.Functions.Like(d.CityName, $"%{city}%")) ||
                (d.CityZip != null && EF.Functions.Like(d.CityZip, $"%{city}%")));
        }

        if (normalized.MinPrice.HasValue)
        {
            query = query.Where(d => d.MinNightlyPrice.HasValue && d.MinNightlyPrice.Value >= normalized.MinPrice.Value);
        }

        if (normalized.MaxPrice.HasValue)
        {
            query = query.Where(d => d.MinNightlyPrice.HasValue && d.MinNightlyPrice.Value <= normalized.MaxPrice.Value);
        }

        if (normalized.MinBedrooms.HasValue)
        {
            query = query.Where(d => d.Bedrooms.HasValue && d.Bedrooms.Value >= normalized.MinBedrooms.Value);
        }

        if (normalized.MinGuests.HasValue)
        {
            query = query.Where(d => d.MaxGuests.HasValue && d.MaxGuests.Value >= normalized.MinGuests.Value);
        }

        if (normalized.HasPool.HasValue)
        {
            query = query.Where(d => d.HasPool == normalized.HasPool.Value);
        }

        if (normalized.PetFriendly.HasValue)
        {
            query = query.Where(d => d.PetFriendly == normalized.PetFriendly.Value);
        }

        if (normalized.AreaId.HasValue && normalized.AreaId.Value != Guid.Empty)
        {
            var areaHouseIds = db.Houses
                .Where(h => h.Areas.Any(a => a.Id == normalized.AreaId.Value))
                .Select(h => h.Id);

            query = query.Where(d => areaHouseIds.Contains(d.HouseId));
        }

        if (normalized.FeatureFilters is { Count: > 0 })
        {
            foreach (var item in normalized.FeatureFilters)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                var featureKey = item.Key.Trim();
                var featureValue = item.Value.Trim();

                query = query.Where(d => db.HouseFeatures.Any(hf =>
                    hf.HouseId == d.HouseId &&
                    hf.Feature != null &&
                    hf.Feature.Key == featureKey &&
                    hf.RawValue == featureValue));
            }
        }

        query = ApplySort(query, normalized);

        var total = await query.CountAsync(ct);
        var docs = await query
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(ct);

        var houseIds = docs.Select(d => d.HouseId).ToArray();

        var imagesByHouse = await db.Images
            .AsNoTracking()
            .Where(i => houseIds.Contains(i.HouseId))
            .GroupBy(i => i.HouseId)
            .ToDictionaryAsync(g => g.Key, g => g
                .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
                .ThenBy(i => i.Id)
                .ToList(), ct);

        var items = docs.Select(d => MapResult(d, imagesByHouse, baseUrl)).ToList();

        return new PageResult<HouseSearchResultDto>
        {
            Query = normalized.Query,
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            Total = total,
            Items = items
        };
    }

    public Task RebuildIndexAsync(CancellationToken ct) => searchIndexer.RebuildAsync(ct);

    public Task UpdateDocumentAsync(Guid houseId, CancellationToken ct) => searchIndexer.UpdateHouseAsync(houseId, ct);

    private static HouseSearchFilter Normalize(HouseSearchFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 5, 50);

        return filter with
        {
            Page = page,
            PageSize = pageSize,
            Query = filter.Query?.Trim(),
            City = filter.City?.Trim()
        };
    }

    private static IQueryable<HouseSearchDocument> ApplySort(IQueryable<HouseSearchDocument> query, HouseSearchFilter filter)
    {
        // SQLite cannot ORDER BY a decimal, so price sorts on its double value (CAST AS REAL on
        // SQLite, float on SQL Server). Houses without a price come last in both directions.
        return filter.Sort switch
        {
            HouseSearchSort.PriceAsc => query
                .OrderBy(d => d.MinNightlyPrice == null)
                .ThenBy(d => (double?)d.MinNightlyPrice)
                .ThenByDescending(d => d.UpdatedAtUtc),
            HouseSearchSort.PriceDesc => query
                .OrderBy(d => d.MinNightlyPrice == null)
                .ThenByDescending(d => (double?)d.MinNightlyPrice)
                .ThenByDescending(d => d.UpdatedAtUtc),
            HouseSearchSort.Newest => query
                .OrderByDescending(d => d.UpdatedAtUtc),
            _ => !string.IsNullOrWhiteSpace(filter.Query)
                ? query
                    .OrderByDescending(d => d.Title == filter.Query)
                    .ThenByDescending(d => d.Title.StartsWith(filter.Query!))
                    .ThenByDescending(d => d.UpdatedAtUtc)
                : query.OrderByDescending(d => d.UpdatedAtUtc)
        };
    }

    private HouseSearchResultDto MapResult(
        HouseSearchDocument document,
        IReadOnlyDictionary<Guid, List<HouseImage>> imagesByHouse,
        string baseUrl)
    {
        imagesByHouse.TryGetValue(document.HouseId, out var images);
        images ??= [];

        var cover = images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
            ?? images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

        var coverUrl = cover is null
            ? null
            : imageStorage.GetUrl(baseUrl, ImageCategory.House, document.HouseId, cover.FileName);

        var gallery = images
            .Where(i => cover is null || i.Id != cover.Id)
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.FileName)
            .Take(5)
            .Select(i => new ImageDto(
                i.Id,
                imageStorage.GetUrl(baseUrl, ImageCategory.House, i.HouseId, i.FileName),
                i.Alt,
                i.Kind.ToString()))
            .ToList();

        var featureValues = ParseFeatureValues(document.FeatureJson);

        return new HouseSearchResultDto(
            Id: document.HouseId,
            Title: document.Title,
            City: document.CityName,
            Zip: document.CityZip,
            Address: document.Address,
            Description: document.Description,
            Images: gallery,
            Features: featureValues,
            MinNightlyPrice: document.MinNightlyPrice,
            Currency: document.Currency,
            CoverUrl: coverUrl,
            Summary: document.Summary,
            Gallery: gallery);
    }

    private static IReadOnlyList<FeatureValueDto> ParseFeatureValues(string? featureJson)
    {
        if (string.IsNullOrWhiteSpace(featureJson))
        {
            return [];
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<SearchFeatureJsonItem>>(featureJson) ?? [];

            return items
                .Select(i => new FeatureValueDto(
                    Id: Guid.Empty,
                    Name: i.Name ?? i.Key,
                    ValueType: ParseFeatureValueType(i.ValueType),
                    Unit: i.Unit,
                    IconUrl: null,
                    RawValue: i.Value))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static FeatureValueType ParseFeatureValueType(string? valueType)
        => Enum.TryParse<FeatureValueType>(valueType, true, out var parsed)
            ? parsed
            : FeatureValueType.Text;

    private sealed record SearchFeatureJsonItem(
        string Key,
        string? Name,
        string Value,
        string? Unit,
        string? Category,
        string? ValueType);
}
