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
    public async Task<PageResult<HouseListItemDto>> Search([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
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
            .Select(h => new HouseListItemDto(h.Id, h.Title, h.City!.Name, null, null, h.CreatedUtc))
            .ToListAsync(ct);

        return new PageResult<HouseListItemDto>
        {
            Query = query ?? "",
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.Area)
            .Include(x => x.HouseFeatures).ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (h is null) return NotFound();

        var images = h.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : (i.Kind == ImageKind.Gallery ? 1 : 2))
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToList();

        var features = h.HouseFeatures
        .Select(hf =>
        {
            var f = hf.Feature;
            return new FeatureValueDto(
                Id: hf.FeatureId,
                Name: f.Name,
                ValueType: f.ValueType.ToString(),
                Unit: f.Unit,
                IconUrl: f.IconUrl,
                RawValue: hf.RawValue
            );
        })
        .ToList();

        return new HouseDetailsDto(h.Id, h.Title, h.CityId, h.City.Name, h.AreaId, h.Area?.Name, h.Address, h.Description, h.CreatedUtc, features, images);
    }


    // POST: /api/admin/houses
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        var entity = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = dto.Name,
            Address = dto.Address,
            CityId = dto.CityId,
            Description = dto.Description,
            CreatedUtc = DateTime.UtcNow
        };

        db.Houses.Add(entity);
        await db.SaveChangesAsync(ct);

        // AdminApiClient forventer et Guid tilbage
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity.Id);
    }

    // PUT: /api/admin/houses/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var h = await db.Houses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        h.Title = dto.Name;
        h.Address = dto.Address;
        h.CityId = dto.CityId;
        h.Description = dto.Description;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }


    // DELETE: /api/admin/houses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var h = await db.Houses.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        db.Houses.Remove(h);
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
