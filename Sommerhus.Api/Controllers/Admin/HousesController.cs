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

        var q = db.Houses.AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Areas)
            .AsQueryable();


        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(h => h.CreatedUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(h => new HouseListItemDto(
                h.Id,
                h.Title,
                h.City != null ? $"{h.City.Zip} – {h.City.Name}" : string.Empty,
                h.Areas.OrderBy(a => a.Name).Select(a => a.Name).ToList(),
                null,
                h.CreatedUtc))
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
            .Include(x => x.Areas)
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

        var areaItems = h.Areas
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToList();

        var areaIds = areaItems.Select(a => a.Id).ToList();

        return new HouseDetailsDto(
            h.Id,
            h.Title,
            h.CityId,
            h.City != null ? $"{h.City.Zip} – {h.City.Name}" : string.Empty,
            areaIds,
            areaItems,
            h.Address,
            h.Description,
            h.CreatedUtc,
            features,
            images);
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


        var requestedAreaIds = dto.AreaIds?.Where(aid => aid != Guid.Empty).Distinct().ToList() ?? new();
        if (dto.AreaIds is not null && dto.AreaIds.Any(aid => aid == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        if (requestedAreaIds.Count > 0)
        {
            var areas = await db.Areas.Where(a => requestedAreaIds.Contains(a.Id)).ToListAsync(ct);
            if (areas.Count != requestedAreaIds.Count)
            {
                ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
                return ValidationProblem(ModelState);
            }

            foreach (var area in areas)
            {
                entity.Areas.Add(area);
            }
        }

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

        var h = await db.Houses
               .Include(x => x.Areas)
               .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        h.Title = dto.Name;
        h.Address = dto.Address;
        h.CityId = dto.CityId;
        h.Description = dto.Description;

        var requestedAreaIds = dto.AreaIds?.Where(aid => aid != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (dto.AreaIds is not null && dto.AreaIds.Any(aid => aid == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        var areas = requestedAreaIds.Count > 0
            ? await db.Areas.Where(a => requestedAreaIds.Contains(a.Id)).ToListAsync(ct)
            : new List<Area>();

        if (areas.Count != requestedAreaIds.Count)
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        h.Areas.Clear();
        foreach (var area in areas)
        {
            h.Areas.Add(area);
        }

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
    public async Task<IActionResult> UpsertFeatures(Guid houseId, [FromBody] IEnumerable<PostFeatureValueDto>? values, CancellationToken ct)
    {

        if (values is null)
            return BadRequest("Feature values are required.");

        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
            return NotFound();

        // Normalize and filter incoming values
        var normalized = values
            .Where(v => v != null)
            .Select(v => new { v.FeatureId, Raw = (v.RawValue ?? string.Empty).Trim() })
            .Where(x => x.FeatureId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Raw))
            .GroupBy(x => x.FeatureId)
            .Select(g => new HouseFeatureValue
            {
                HouseId = houseId,
                FeatureId = g.Key,
                RawValue = g.First().Raw
            })
            .ToList();

        var featureIds = normalized.Select(i => i.FeatureId).Distinct().ToList();
        if (featureIds.Count > 0)
        {
            var existingFeatureIds = await db.Features
                .AsNoTracking()
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(ct);

            var missing = featureIds.Except(existingFeatureIds).ToList();
            if (missing.Any())
            {
                return BadRequest(new { Message = "Some features do not exist.", MissingFeatureIds = missing });
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Efficient server-side delete of existing values for this house
            await db.HouseFeatures.Where(hf => hf.HouseId == houseId).ExecuteDeleteAsync(ct);

            if (normalized.Count > 0)
            {
                await db.HouseFeatures.AddRangeAsync(normalized, ct);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
