using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Api.Models;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<HouseListItemDto>> Search(
        [FromQuery] string? city,
        [FromQuery] string? zip,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var query = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .Include(h => h.City)
            .Include(h => h.Areas)
            .Include(h => h.HouseFeatures).ThenInclude(v => v.Feature)
            .OrderByDescending(h => h.CreatedUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim();
            query = query.Where(h =>
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(zip))
        {
            var term = zip.Trim();
            query = query.Where(h => h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return list.Select(h =>
        {
            var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                     ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

            string? coverUrl = cover is null
                ? null
                : storage.GetUrl(Request, ImageCategory.House, cover.HouseId, cover.FileName);

            var gallery = h.Images
                .Where(i => cover is null || i.Id != cover.Id)
                .OrderBy(i => i.Kind)
                .ThenBy(i => i.FileName)
                .Take(5)
                .Select(i => new ImageDto(
                    i.Id,
                    storage.GetUrl(Request, ImageCategory.House, i.HouseId, i.FileName),
                    i.Alt,
                    i.Kind.ToString()))
                .ToArray();

            return new HouseListItemDto(
                h.Id,
                h.Title,
                h.City?.Name ?? string.Empty,
                h.City?.Zip ?? string.Empty,
                coverUrl,
                HouseSummaryFormatter.BuildSummary(h),
                gallery);
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.HouseFeatures).ThenInclude(v => v.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (h is null) return NotFound();

        var gallery = h.Images
            .Select(i => new ImageDto(i.Id, storage.GetUrl(Request, ImageCategory.House, i.HouseId, i.FileName), i.Alt, i.Kind.ToString()))
            .ToArray();

        var features = h.HouseFeatures
            .Select(hf =>
            {
                var f = hf.Feature;
                var icon = storage.GetUrl(Request, f?.IconUrl);
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
            h.Id,
            h.Title,
            h.City?.Name,
            h.City?.Zip,
            h.Address,
            h.Description,
            gallery,
            features);
    }
}

static class HouseSummaryFormatter
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

        var maxLength = 160;
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
