using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Api.Data;
using Sommerhus.Contracts.Dtos.Admin.Features;
using System.Collections.Generic;
using System.Linq;


namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(AppDbContext db) : ControllerBase
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
                : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(cover.HouseId, cover.FileName));

            return new HouseListItemDto(h.Id, h.Title, h.City.Name, h.City.Zip, coverUrl);
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
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToArray();


        FeatureDetailsDto Map(HouseFeatureValue v)
        {
            var f = v.Feature!;
            var display = string.IsNullOrWhiteSpace(v.RawValue) ? string.Empty :
                f.ValueType == FeatureValueType.Bool
                    ? (bool.TryParse(v.RawValue, out var b) ? (b ? "Ja" : "Nej") : string.Empty)
                    : v.RawValue ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(f.Unit) && !string.IsNullOrWhiteSpace(display))
                display = $"{display} {f.Unit}";

            return new FeatureDetailsDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl);
        }

        return new HouseDetailsDto(
            h.Id,
            h.Title,
            h.City?.Name,
            h.City?.Zip,
            h.Address,
            h.Description,
            gallery,
            h.HouseFeatures.Select(Map).ToArray());
    }
}
