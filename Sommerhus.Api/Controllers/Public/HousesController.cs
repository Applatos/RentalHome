using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Api.Data;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<HouseListItemDto>> GetAll(
        [FromQuery] string? city,
        [FromQuery] string? zip,
        [FromQuery] string? q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        take = take <= 0 ? 50 : Math.Min(take, 100);

        var query = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .Include(h => h.City)
            .OrderByDescending(h => h.CreatedUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim();
            var slugTerm = term.ToLowerInvariant(); // <- normalize slug
            query = query.Where(h =>
                (h.City != null && h.City.Slug != null && h.City.Slug == slugTerm) ||
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
                (h.Subtitle != null && EF.Functions.Like(h.Subtitle, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")) ||
                (h.City != null && h.City.Slug != null && EF.Functions.Like(h.City.Slug, $"%{term}%")));
        }

        var list = await query.Skip(Math.Max(0, skip)).Take(take).ToListAsync(ct);

        return list.Select(h =>
        {
            var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                     ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

            string? coverUrl = cover is null
                ? null
                : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(cover.HouseId, cover.FileName));

            return new HouseListItemDto(h.Id, h.Title, h.Subtitle, h.City?.Name, h.City?.Zip, coverUrl);
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

        var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                 ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

        var coverUrl = cover is null ? null : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(cover.HouseId, cover.FileName));

        var gallery = h.Images.Where(i => i.Kind == ImageKind.Gallery || i.Kind == ImageKind.Cover)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : 1)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToArray();

        var floor = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Floorplan);
        var floorDto = floor is null ? null
            : new ImageDto(floor.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(floor.HouseId, floor.FileName)), floor.Alt, floor.Kind.ToString());

        FeatureValueDto Map(HouseFeatureValue v)
        {
            var f = v.Feature!;
            var display = string.IsNullOrWhiteSpace(v.RawValue) ? string.Empty :
                f.ValueType == FeatureValueType.Bool
                    ? (bool.TryParse(v.RawValue, out var b) ? (b ? "Ja" : "Nej") : string.Empty)
                    : v.RawValue ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(f.Unit) && !string.IsNullOrWhiteSpace(display))
                display = $"{display} {f.Unit}";

            return new FeatureValueDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, display);
        }

        return new HouseDetailsDto(
            h.Id,
            h.Title,
            h.Subtitle,
            h.City?.Name,
            h.City?.Zip,
            h.Address,
            h.City?.Slug,
            h.Description,
            h.Facilities,
            coverUrl,
            gallery,
            floorDto,
            h.HouseFeatures.Select(Map).OrderBy(x => x.Name).ToArray());
    }
}
