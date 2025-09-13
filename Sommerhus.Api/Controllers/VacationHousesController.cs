//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Sommerhus.Api.Controllers.Models;
//using Sommerhus.Api.Data;
//using Sommerhus.Api.Models;
//using System.Linq;

//namespace Sommerhus.Api.Controllers;

//[ApiController]
//[Route("api/[controller]")] // => /api/VacationHouses
//public class VacationHousesController(AppDbContext db) : ControllerBase
//{
//    // LISTE
//    [HttpGet]
//    public async Task<IEnumerable<HouseListItemDto>> GetAll(CancellationToken ct)
//    {
//        // Brug alias-DbSet så gammel routing/navn stadig virker
//        var raw = await db.VacationHouses.AsNoTracking()
//            .Select(h => new { h.Id, h.Title, h.Subtitle, h.City, h.Zip })
//            .ToListAsync(ct);

//        var covers = await db.VacationImages.AsNoTracking()
//            .Where(i => i.Kind == ImageKind.Cover)
//            .GroupBy(i => i.HouseId)
//            .Select(g => new { g.Key, Image = g.First() })
//            .ToDictionaryAsync(x => x.Key, x => x.Image, ct);

//        return raw.Select(h =>
//        {
//            covers.TryGetValue(h.Id, out var img);
//            var url = img is null ? null : $"/uploads/houses/{img.HouseId}/{img.FileName}";
//            return new HouseListItemDto(h.Id, h.Title, h.Subtitle, h.City, h.Zip, url);
//        });
//    }

//    // DETALJE
//    [HttpGet("{id:guid}")]
//    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
//    {
//        var h = await db.VacationHouses.Include(x => x.Images).AsNoTracking()
//            .FirstOrDefaultAsync(x => x.Id == id, ct);
//        if (h == null) return NotFound();

//        var cover = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Cover);
//        var gallery = h.Images.Where(i => i.Kind == ImageKind.Gallery).ToArray();
//        var floor = h.Images.FirstOrDefault(i => i.Kind == ImageKind.Floorplan);

//        // features
//        var fv = await db.HouseFeatureValues
//            .Where(v => v.HouseId == id)
//            .Include(v => v.Feature!)
//            .AsNoTracking()
//            .OrderBy(v => v.Feature!.SortOrder).ThenBy(v => v.Feature!.Name)
//            .ToListAsync(ct);

//        string Disp(HouseFeatureValue v)
//        {
//            var vt = v.Feature!.ValueType.ToString();
//            var unit = v.Feature!.Unit;
//            return vt switch
//            {
//                nameof(FeatureValueType.Bool) => (v.ValueBool ?? false) ? "Ja" : "Nej",
//                nameof(FeatureValueType.Int) => v.ValueInt.HasValue ? (unit is null ? $"{v.ValueInt}" : $"{v.ValueInt} {unit}") : "",
//                nameof(FeatureValueType.Decimal) => v.ValueDecimal.HasValue ? (unit is null ? $"{v.ValueDecimal}" : $"{v.ValueDecimal} {unit}") : "",
//                _ => v.ValueText ?? ""
//            };
//        }

//        var featureDtos = fv.Select(v => new FeatureValueDto(
//            v.FeatureId, v.Feature!.Name, v.Feature!.Key, v.Feature!.ValueType.ToString(), v.Feature!.Unit,
//            v.ValueBool, v.ValueInt, v.ValueDecimal, v.ValueText, Disp(v)
//        )).ToArray();

//        return new HouseDetailsDto(
//            h.Id, h.Title, h.Subtitle, h.City, h.Zip, h.Description, h.Facilities,
//            cover != null ? $"/uploads/houses/{cover.HouseId}/{cover.FileName}" : null,
//            gallery.Select(i => new HouseImageDto(i.Id, $"/uploads/houses/{i.HouseId}/{i.FileName}", i.Alt, i.Kind.ToString())).ToArray(),
//            floor != null ? new HouseImageDto(floor.Id, $"/uploads/houses/{floor.HouseId}/{floor.FileName}", floor.Alt, floor.Kind.ToString()) : null,
//            featureDtos
//        );
//    }

//    // CREATE (bevarer Zip/City/Address)
//    public record CreateHouseDto(string Title, string? Subtitle, string? Address, string? City, string? Zip,
//        string? Description, string? Facilities);

//    [HttpPost]
//    public async Task<ActionResult> Create([FromBody] CreateHouseDto dto, CancellationToken ct)
//    {
//        var h = new VacationHouse
//        {
//            Title = dto.Title,
//            Subtitle = dto.Subtitle,
//            Address = dto.Address,
//            City = dto.City,
//            Zip = dto.Zip,
//            Description = dto.Description,
//            Facilities = dto.Facilities
//        };
//        db.VacationHouses.Add(h);
//        await db.SaveChangesAsync(ct);
//        return CreatedAtAction(nameof(Get), new { id = h.Id }, new { h.Id });
//    }

//    // UPDATE
//    [HttpPut("{id:guid}")]
//    public async Task<IActionResult> Update(Guid id, [FromBody] CreateHouseDto dto, CancellationToken ct)
//    {
//        var h = await db.VacationHouses.FirstOrDefaultAsync(x => x.Id == id, ct);
//        if (h == null) return NotFound();

//        h.Title = dto.Title; h.Subtitle = dto.Subtitle;
//        h.Address = dto.Address; h.City = dto.City; h.Zip = dto.Zip;
//        h.Description = dto.Description; h.Facilities = dto.Facilities;

//        await db.SaveChangesAsync(ct);
//        return NoContent();
//    }

//    // DELETE
//    [HttpDelete("{id:guid}")]
//    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
//    {
//        var h = await db.VacationHouses.FirstOrDefaultAsync(x => x.Id == id, ct);
//        if (h == null) return NotFound();
//        db.VacationHouses.Remove(h);
//        await db.SaveChangesAsync(ct);
//        return NoContent();
//    }
//}
