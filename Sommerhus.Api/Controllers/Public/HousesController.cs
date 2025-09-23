using Sommerhus.Api.Dtos.Public.Houses; // HouseListItemDto, HouseDetailsDto
using Sommerhus.Api.Dtos.Shared;        // ImageDto
using Sommerhus.Api.Utils;              // UrlBuilder
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<HouseListItemDto>> GetAll(
        [FromQuery] string? city, [FromQuery] string? zip, [FromQuery] string? q,
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var query = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .Include(h => h.City)
            .OrderByDescending(h => h.CreatedUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(h => h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{city}%"));

        if (!string.IsNullOrWhiteSpace(zip))
            query = query.Where(h => h.City.Zip != null && h.City.Zip.Contains(zip));

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(h =>
                (h.Title != null && h.Title.Contains(q)) ||
                (h.Subtitle != null && h.Subtitle.Contains(q)) ||
                (h.Description != null && h.Description.Contains(q)) ||
                (h.City != null && h.City.Name.Contains(q)) ||
                (h.City != null && h.City.Zip.Contains(q)));

        var list = await query.Skip(skip).Take(take).ToListAsync(ct);

        return list.Select(h =>
        {
            var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                     ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

            string? coverUrl = cover is null ? null : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(cover.HouseId, cover.FileName));
            return new HouseListItemDto(h.Id, h.Title, h.Subtitle, h.City.Name, h.City.Zip, coverUrl);
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

        var gallery = h.Images.Where(i => i.Kind == ImageKind.Gallery)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToArray();

        var floor = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Floorplan);
        var floorDto = floor is null ? null
            : new ImageDto(floor.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(floor.HouseId, floor.FileName)), floor.Alt, floor.Kind.ToString());

        FeatureValueDto Map(HouseFeatureValue v)
        {
            var f = v.Feature!;
            var display = string.IsNullOrWhiteSpace(v.RawValue) ? "" :
                f.ValueType == FeatureValueType.Bool
                    ? (bool.TryParse(v.RawValue, out var b) ? (b ? "Ja" : "Nej") : "")
                    : v.RawValue;

            if (!string.IsNullOrWhiteSpace(f.Unit) && !string.IsNullOrWhiteSpace(display))
                display = $"{display} {f.Unit}";

            return new FeatureValueDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, display);
        }

        return new HouseDetailsDto(
            h.Id, h.Title, h.Subtitle, h.CityId, h.Description, h.Facilities, coverUrl,
            gallery, floorDto, h.HouseFeatures.Select(Map).OrderBy(x => x.Name).ToArray());
    }

    // Admin-agtige endpoints (Create/Update/Delete) bør fortsat ligge i Admin, så public holdes read-only.
}
