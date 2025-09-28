using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<HousesPageDto> Search([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var q = db.Houses.AsNoTracking().Include(h => h.City).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Subtitle != null && EF.Functions.Like(h.Subtitle, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(h => h.CreatedUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(h => new HouseRowDto(h.Id, h.Title, h.City!.Name, h.CreatedUtc))
            .ToListAsync(ct);

        return new HousesPageDto(query ?? "", page, pageSize, total, rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseAdminDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (h is null) return NotFound();

        var images = h.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : (i.Kind == ImageKind.Gallery ? 1 : 2))
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToList();

        var cityMini = h.City is null ? null : new CityMiniDto(h.City.Name, h.City.Zip);

        return new HouseAdminDetailsDto(h.Id, h.Title, cityMini, h.Address, h.CreatedUtc, images);
    }

    [HttpDelete("{houseId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();

        db.Images.Remove(img);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{houseId:guid}/features")]
    public async Task<IActionResult> UpsertFeatures(Guid houseId, [FromBody] IEnumerable<PostFeatureValueDto> values, CancellationToken ct)
    {
        var exists = await db.Houses.AnyAsync(h => h.Id == houseId, ct);
        if (!exists) return NotFound();

        // simple replace-strategy
        await db.Database.ExecuteSqlRawAsync("DELETE FROM HouseFeatureValues WHERE HouseId = {0}", houseId);

        var items = values?.Select(v => new HouseFeatureValue
        {
            HouseId = houseId,
            FeatureId = v.FeatureId,
            RawValue = (v.RawValue ?? "").Trim()
        }) ?? Enumerable.Empty<HouseFeatureValue>();

        db.HouseFeatures.AddRange(items);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
