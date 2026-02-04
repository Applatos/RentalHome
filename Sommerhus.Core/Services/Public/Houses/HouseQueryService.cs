using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Houses;

public sealed class HouseQueryService : IHouseQueryService
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private readonly AppDbContext db;
    private readonly IImageStorage storage;

    public HouseQueryService(AppDbContext db, IImageStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    public async Task<IEnumerable<HouseListItemDto>> SearchAsync(string? city, string? zip, string? query, Guid? areaId, int page, int pageSize, HttpRequest request, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var houseQuery = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .Include(h => h.City)
            .Include(h => h.Areas)
            .Include(h => h.HouseFeatures).ThenInclude(v => v.Feature)
            .OrderByDescending(h => h.CreatedUtc)
            .AsQueryable();

        if (areaId.HasValue && areaId.Value != Guid.Empty)
        {
            houseQuery = houseQuery.Where(h => h.Areas.Any(a => a.Id == areaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim();
            houseQuery = houseQuery.Where(h =>
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(zip))
        {
            var term = zip.Trim();
            houseQuery = houseQuery.Where(h => h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            houseQuery = houseQuery.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var houses = await houseQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return houses.Select(h =>
        {
            var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

            string? coverUrl = cover is null
                ? null
                : storage.GetUrl(request, ImageCategory.House, cover.HouseId, cover.FileName);

            var gallery = h.Images
                .Where(i => cover is null || i.Id != cover.Id)
                .OrderBy(i => i.Kind)
                .ThenBy(i => i.FileName)
                .Take(5)
                .Select(i => new ImageDto(
                    i.Id,
                    storage.GetUrl(request, ImageCategory.House, i.HouseId, i.FileName),
                    i.Alt,
                    i.Kind.ToString()))
                .ToArray();

            return new HouseListItemDto(
                h.Id,
                h.Title,
                h.City?.Name,
                h.City?.Zip,
                h.Address,
                h.Description,
                gallery,
                new List<FeatureValueDto>(),
                
                // Public-specific fields
                CoverUrl: coverUrl,
                Summary: BuildSummary(h),
                Gallery: gallery);
        }).ToList();
    }

    public async Task<HouseDetailsDto?> GetAsync(Guid id, HttpRequest request, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.HouseFeatures).ThenInclude(v => v.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (house is null)
        {
            return null;
        }

        var gallery = house.Images
            .Select(i => new ImageDto(i.Id, storage.GetUrl(request, ImageCategory.House, i.HouseId, i.FileName), i.Alt, i.Kind.ToString()))
            .ToArray();

        var features = house.HouseFeatures
            .Select(hf =>
            {
                var f = hf.Feature;
                var icon = storage.GetUrl(request, f?.IconUrl);
                return new FeatureValueDto(
                    Id: hf.FeatureId,
                    Name: f?.Name ?? string.Empty,
                    ValueType: f?.ValueType.ToString() ?? string.Empty,
                    Unit: f?.Unit,
                    IconUrl: icon,
                    RawValue: hf.RawValue
                );
            })
            .ToList();

        return new HouseDetailsDto(
            house.Id,
            house.Title,
            house.City?.Name,
            house.City?.Zip,
            house.Address,
            house.Description,
            gallery,
            features);
    }

    private static string? BuildSummary(VacationHouse house)
    {
        var source = string.IsNullOrWhiteSpace(house.Description)
            ? house.Facilities
            : house.Description;

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
}
