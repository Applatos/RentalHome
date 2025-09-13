using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Controllers.Models;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HousesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static string ImgPath(Guid houseId, string fileName)
        => $"/uploads/houses/{houseId}/{fileName}";

    private string AbsUrl(string relative)
        => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{relative}";

    [HttpGet]
    public async Task<IEnumerable<HouseListItemDto>> GetAll(
        [FromQuery] string? city, [FromQuery] string? zip, [FromQuery] string? q,
        [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var query = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .OrderByDescending(h => h.CreatedUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(h => h.City != null && h.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(zip))
            query = query.Where(h => h.Zip != null && h.Zip.Contains(zip));

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(h =>
                (h.Title != null && h.Title.Contains(q)) ||
                (h.Subtitle != null && h.Subtitle.Contains(q)) ||
                (h.Description != null && h.Description.Contains(q)) ||
                (h.City != null && h.City.Contains(q)) ||
                (h.Zip != null && h.Zip.Contains(q)));

        var list = await query.Skip(skip).Take(take).ToListAsync(ct);

        return list.Select(h =>
        {
            var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                        ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);

            string? coverUrl = cover is null ? null : AbsUrl(ImgPath(cover.HouseId, cover.FileName));

            return new HouseListItemDto(
                h.Id, h.Title, h.Subtitle, h.City, h.Zip, coverUrl
            );
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.HouseFeatures).ThenInclude(v => v.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (h is null) return NotFound();

        var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover)
                    ?? h.Images.FirstOrDefault(i => i.Kind == ImageKind.Gallery);
        var gallery = h.Images.Where(i => i.Kind == ImageKind.Gallery).ToList();
        var floor = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Floorplan);

        FeatureValueDto Map(HouseFeatureValue v)
        {
            var f = v.Feature!;
            string vt = f.ValueType.ToString();
            string? unit = f.Unit;
            string display = f.ValueType switch
            {
                FeatureValueType.Bool => v.ValueBool == true ? "Ja" : "Nej",
                FeatureValueType.Int => v.ValueInt?.ToString() ?? "",
                FeatureValueType.Decimal => v.ValueDecimal?.ToString() ?? "",
                FeatureValueType.Text => v.ValueText ?? "",
                _ => ""
            };
            if (!string.IsNullOrWhiteSpace(unit) && !string.IsNullOrWhiteSpace(display))
                display = $"{display} {unit}";
            return new FeatureValueDto(f.Id, f.Name, f.Key, vt, f.Unit, f.IconUrl,
                v.ValueBool, v.ValueInt, v.ValueDecimal, v.ValueText, display);
        }

        var coverUrl = cover is null ? null : AbsUrl(ImgPath(cover.HouseId, cover.FileName));

        return new HouseDetailsDto(
            h.Id, h.Title, h.Subtitle, h.City, h.Zip, h.Description, h.Facilities,
            coverUrl,
            gallery.Select(i => new HouseImageDto(i.Id, AbsUrl(ImgPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString())).ToArray(),
            floor is null ? null : new HouseImageDto(floor.Id, AbsUrl(ImgPath(floor.HouseId, floor.FileName)), floor.Alt, floor.Kind.ToString()),
            h.HouseFeatures.Select(Map).OrderBy(x => x.Name).ToArray()
        );
    }

    public record CreateHouseDto(string Title, string? Subtitle, string? Address, string? City, string? Zip,
        string? Description, string? Facilities);
    public record UpdateHouseDto(string Title, string? Subtitle, string? Address, string? City, string? Zip,
        string? Description, string? Facilities);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateHouseDto dto, CancellationToken ct)
    {
        var h = new VacationHouse
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            Address = dto.Address,
            City = dto.City,
            Zip = dto.Zip,
            Description = dto.Description,
            Facilities = dto.Facilities
        };
        db.Houses.Add(h);
        await db.SaveChangesAsync(ct);
        return Ok(h.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHouseDto dto, CancellationToken ct)
    {
        var h = await db.Houses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        h.Title = dto.Title;
        h.Subtitle = dto.Subtitle;
        h.Address = dto.Address;
        h.City = dto.City;
        h.Zip = dto.Zip;
        h.Description = dto.Description;
        h.Facilities = dto.Facilities;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var h = await db.Houses.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        var dir = Path.Combine(env.WebRootPath, "uploads", "houses", h.Id.ToString());
        if (Directory.Exists(dir)) Directory.Delete(dir, true);

        db.Images.RemoveRange(h.Images);
        db.Houses.Remove(h);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
