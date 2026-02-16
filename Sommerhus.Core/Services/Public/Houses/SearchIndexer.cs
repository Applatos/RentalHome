using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Public.Houses;

public sealed class SearchIndexer(AppDbContext db) : ISearchIndexer
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    public async Task RebuildAsync(CancellationToken ct)
    {
        var houses = await BuildHouseQuery()
            .AsNoTracking()
            .ToListAsync(ct);

        var houseIds = houses.Select(h => h.Id).ToList();

        if (houseIds.Count == 0)
        {
            await db.HouseSearchDocuments.ExecuteDeleteAsync(ct);
            return;
        }

        var summariesByHouseId = await db.HousePriceSummaries
            .AsNoTracking()
            .Where(s => houseIds.Contains(s.HouseId))
            .ToDictionaryAsync(s => s.HouseId, ct);

        var existingByHouseId = await db.HouseSearchDocuments
            .Where(d => houseIds.Contains(d.HouseId))
            .ToDictionaryAsync(d => d.HouseId, ct);

        await db.HouseSearchDocuments
            .Where(d => !houseIds.Contains(d.HouseId))
            .ExecuteDeleteAsync(ct);

        foreach (var house in houses)
        {
            summariesByHouseId.TryGetValue(house.Id, out var summary);
            var mapped = MapDocument(house, summary);

            if (existingByHouseId.TryGetValue(house.Id, out var existing))
            {
                CopyDocument(mapped, existing);
                continue;
            }

            db.HouseSearchDocuments.Add(mapped);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateHouseAsync(Guid houseId, CancellationToken ct)
    {
        var house = await BuildHouseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == houseId, ct);

        if (house is null)
        {
            await RemoveHouseAsync(houseId, ct);
            return;
        }

        var summary = await db.HousePriceSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HouseId == houseId, ct);

        var mapped = MapDocument(house, summary);

        var existing = await db.HouseSearchDocuments
            .FirstOrDefaultAsync(d => d.HouseId == houseId, ct);

        if (existing is null)
        {
            db.HouseSearchDocuments.Add(mapped);
        }
        else
        {
            CopyDocument(mapped, existing);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveHouseAsync(Guid houseId, CancellationToken ct)
    {
        await db.HouseSearchDocuments
            .Where(d => d.HouseId == houseId)
            .ExecuteDeleteAsync(ct);
    }

    private IQueryable<VacationHouse> BuildHouseQuery()
        => db.Houses
            .Include(h => h.City)
            .Include(h => h.Areas)
            .Include(h => h.Images)
            .Include(h => h.HouseFeatures)
                .ThenInclude(v => v.Feature);

    private static HouseSearchDocument MapDocument(VacationHouse house, HousePriceSummary? summary)
    {
        var coverImageFileName = house.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.Id)
            .Select(i => i.FileName)
            .FirstOrDefault();

        var areaNames = house.Areas
            .OrderBy(a => a.Name)
            .Select(a => a.Name.Trim())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        var searchableFeatures = house.HouseFeatures
            .Where(hf => hf.Feature?.IsSearchable == true)
            .Select(hf => new SearchFeatureEntry(
                hf.Feature!.Key,
                hf.Feature.Name,
                hf.RawValue,
                hf.Feature.Unit,
                hf.Feature.Category.ToString(),
                hf.Feature.ValueType.ToString()))
            .ToList();

        var featureJson = searchableFeatures.Count == 0
            ? null
            : JsonSerializer.Serialize(searchableFeatures);

        return new HouseSearchDocument
        {
            HouseId = house.Id,
            Title = house.Title,
            Description = house.Description,
            Summary = BuildSummary(house.Description),
            CityName = house.City?.Name,
            CityZip = house.City?.Zip,
            Address = house.Address,
            AreaNames = areaNames.Count == 0 ? null : string.Join(", ", areaNames),
            SearchKeywords = house.SearchKeywords,
            FeatureJson = featureJson,
            CoverImageUrl = coverImageFileName,
            Status = house.Status,
            MinNightlyPrice = summary?.MinNightlyPrice,
            MaxNightlyPrice = summary?.MaxNightlyPrice,
            Currency = summary?.Currency,
            Bedrooms = TryExtractIntFeature(house, "bedrooms"),
            MaxGuests = TryExtractIntFeature(house, "max_guests", "guests", "maxguests"),
            HasPool = TryExtractBoolFeature(house, "pool"),
            PetFriendly = TryExtractBoolFeature(house, "pet_friendly", "pets_allowed", "pets"),
            Latitude = null,
            Longitude = null,
            SearchVector = BuildSearchVector(house, areaNames, searchableFeatures),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static void CopyDocument(HouseSearchDocument source, HouseSearchDocument target)
    {
        target.Title = source.Title;
        target.Description = source.Description;
        target.Summary = source.Summary;
        target.CityName = source.CityName;
        target.CityZip = source.CityZip;
        target.Address = source.Address;
        target.AreaNames = source.AreaNames;
        target.SearchKeywords = source.SearchKeywords;
        target.FeatureJson = source.FeatureJson;
        target.CoverImageUrl = source.CoverImageUrl;
        target.Status = source.Status;
        target.MinNightlyPrice = source.MinNightlyPrice;
        target.MaxNightlyPrice = source.MaxNightlyPrice;
        target.Currency = source.Currency;
        target.Bedrooms = source.Bedrooms;
        target.MaxGuests = source.MaxGuests;
        target.HasPool = source.HasPool;
        target.PetFriendly = source.PetFriendly;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
        target.SearchVector = source.SearchVector;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }

    private static int? TryExtractIntFeature(VacationHouse house, params string[] keys)
    {
        var value = TryGetFeatureRawValue(house, keys);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool TryExtractBoolFeature(VacationHouse house, params string[] keys)
    {
        var value = TryGetFeatureRawValue(house, keys);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        var normalized = value.Trim();
        return normalized is "1" or "yes" or "y" or "true";
    }

    private static string? TryGetFeatureRawValue(VacationHouse house, params string[] keys)
    {
        foreach (var key in keys)
        {
            var feature = house.HouseFeatures
                .FirstOrDefault(f =>
                    string.Equals(f.Feature?.Key, key, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(feature?.RawValue))
            {
                return feature.RawValue;
            }
        }

        return null;
    }

    private static string BuildSearchVector(
        VacationHouse house,
        IEnumerable<string> areaNames,
        IEnumerable<SearchFeatureEntry> features)
    {
        var featureTokens = features
            .SelectMany(f => new[] { f.Key, f.Name, f.Value, f.Unit })
            .Where(v => !string.IsNullOrWhiteSpace(v));

        var allTokens = new[]
            {
                house.Title,
                house.Description,
                house.Address,
                house.City?.Name,
                house.City?.Zip,
                house.SearchKeywords
            }
            .Concat(areaNames)
            .Concat(featureTokens)
            .Where(v => !string.IsNullOrWhiteSpace(v));

        var text = string.Join(' ', allTokens);
        var normalized = WhitespaceRegex.Replace(HtmlTagRegex.Replace(text, " "), " ").Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? house.Title
            : normalized;
    }

    private static string? BuildSummary(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var withoutHtml = HtmlTagRegex.Replace(source, " ");
        var normalized = WhitespaceRegex.Replace(withoutHtml, " ").Trim();

        if (normalized.Length == 0)
        {
            return null;
        }

        const int maxLength = 160;
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        foreach (var endChar in new[] { '.', '!', '?' })
        {
            var sentenceEnd = normalized.IndexOf(endChar);
            if (sentenceEnd >= 0 && sentenceEnd + 1 <= maxLength)
            {
                return normalized[..(sentenceEnd + 1)].Trim();
            }
        }

        return normalized[..maxLength].TrimEnd();
    }

    private sealed record SearchFeatureEntry(
        string Key,
        string Name,
        string Value,
        string? Unit,
        string Category,
        string ValueType);
}
