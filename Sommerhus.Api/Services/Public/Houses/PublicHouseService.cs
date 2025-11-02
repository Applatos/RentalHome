using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Services.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sommerhus.Api.Services.Public.Houses;

public sealed class PublicHouseService
{
    private readonly AppDbContext db;

    public PublicHouseService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<HouseListItemDto>> SearchAsync(string? city, string? zip, string? query, int page, int pageSize, HttpRequest request, CancellationToken ct)
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

        var houses = await houseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return houses.Select(h => MapListItem(h, request)).ToList();
    }

    public async Task<ServiceResult<HouseDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.HouseFeatures).ThenInclude(v => v.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (house is null)
        {
            return ServiceResult<HouseDetailsDto>.NotFound();
        }

        return ServiceResult<HouseDetailsDto>.Success(MapDetails(house, request));
    }

    private static HouseListItemDto MapListItem(VacationHouse house, HttpRequest request)
    {
        var cover = house.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                 ?? house.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

        string? coverUrl = cover is null
            ? null
            : UrlBuilder.ToAbsolute(request, UrlBuilder.HouseImageWebPath(cover.HouseId, cover.FileName));

        var gallery = house.Images
            .Where(i => cover is null || i.Id != cover.Id)
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.FileName)
            .Take(5)
            .Select(i => new ImageDto(
                i.Id,
                UrlBuilder.ToAbsolute(request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)),
                i.Alt,
                i.Kind.ToString()))
            .ToArray();

        return new HouseListItemDto(
            house.Id,
            house.Title,
            house.City?.Name ?? string.Empty,
            house.City?.Zip ?? string.Empty,
            coverUrl,
            HouseSummaryBuilder.BuildSummary(house),
            gallery);
    }

    private static HouseDetailsDto MapDetails(VacationHouse house, HttpRequest request)
    {
        var gallery = house.Images
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToArray();

        var features = house.HouseFeatures
            .Select(hf =>
            {
                var f = hf.Feature;
                var icon = string.IsNullOrWhiteSpace(f?.IconUrl) ? null : UrlBuilder.ToAbsolute(request, f!.IconUrl);
                return new FeatureValueDto(
                    hf.FeatureId,
                    f?.Name ?? string.Empty,
                    f?.ValueType.ToString() ?? string.Empty,
                    f?.Unit,
                    icon,
                    hf.RawValue);
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

    private static class HouseSummaryBuilder
    {
        private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

        public static string? BuildSummary(VacationHouse house)
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
}