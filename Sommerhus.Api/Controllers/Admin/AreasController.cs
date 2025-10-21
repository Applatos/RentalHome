using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/areas")]
public sealed class AreasController(AppDbContext db) : ControllerBase
{

    [HttpGet]
    public async Task<IEnumerable<AreaListItemDto>> GetAll(CancellationToken ct)
        => await db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AreaListItemDto(a.Id, a.Name, a.Houses.Count))
            .ToListAsync(ct);



    [HttpGet("lookup")]
    public async Task<IReadOnlyList<LookupItem>> Lookup(CancellationToken ct)
        => await db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToListAsync(ct);



    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var area = await db.Areas
            .Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        return MapDetails(area);
    }


    [HttpPost]
    public async Task<ActionResult<AreaDetailsDto>> Create([FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Navn er påkrævet");
            return ValidationProblem(ModelState);
        }


        var requestedCityIds = dto.CityIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (dto.CityIds is not null && dto.CityIds.Any(id => id == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.CityIds), "Ukendt by");
            return ValidationProblem(ModelState);
        }

        var cities = requestedCityIds.Count > 0
        ? await db.Cities.Where(c => requestedCityIds.Contains(c.Id)).ToListAsync(ct)
        : new List<City>();

        if (cities.Count != requestedCityIds.Count)
        {
            ModelState.AddModelError(nameof(dto.CityIds), "Ukendt by");
            return ValidationProblem(ModelState);
        }


        var area = new Area
        {
            Name = name,
            Description = NormalizeDescription(dto.Description),
        };

        foreach (var city in cities)
        {
            area.Cities.Add(city);
        }


        var images = NormalizeImages(dto.Images).ToList();
        if (images.Count > 0)
        {
            foreach (var img in images)
                img.AreaId = area.Id;
            area.AreaImages = images;
        }

        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);

        var created = await db.Areas.Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstAsync(a => a.Id == area.Id, ct);

        var details = MapDetails(created);
        return CreatedAtAction(nameof(Get), new { id = details.Id }, details);
    }





    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var area = await db.Areas.Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Navn er påkrævet");
            return ValidationProblem(ModelState);
        }

        var requestedCityIds = dto.CityIds?.Where(cid => cid != Guid.Empty).Distinct().ToList();
        if (dto.CityIds is not null && dto.CityIds.Any(cid => cid == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.CityIds), "Ukendt by");
            return ValidationProblem(ModelState);
        }


        var cities = requestedCityIds.Count > 0
            ? await db.Cities.Where(c => requestedCityIds.Contains(c.Id)).ToListAsync(ct)
            : new List<City>();

        if (cities.Count != requestedCityIds.Count)
        {
            ModelState.AddModelError(nameof(dto.CityIds), "Ukendt by");
            return ValidationProblem(ModelState);
        }


        area.Name = name;
        area.Description = NormalizeDescription(dto.Description);

        area.Cities.Clear();
        foreach (var city in cities)
        {
            area.Cities.Add(city);
        }


        if (dto.Images is not null)
        {
            db.AreaImages.RemoveRange(area.AreaImages);
            var updatedImages = NormalizeImages(dto.Images).ToList();
            foreach (var img in updatedImages)
                img.AreaId = area.Id;
            area.AreaImages = updatedImages;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var area = await db.Areas.Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        db.Areas.Remove(area);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static IEnumerable<AreaImage> NormalizeImages(IReadOnlyList<string>? images)
    {
        if (images is null || images.Count == 0)
            yield break;

        var order = 0;
        foreach (var img in images)
        {
            if (string.IsNullOrWhiteSpace(img)) continue;
            yield return new AreaImage
            {
                FileName = img.Trim(),
                SortOrder = order++
            };
        }
    }

    private AreaDetailsDto MapDetails(Area area)
    {
        var houses = area.Houses
                   .OrderBy(h => h.Title)
                   .ThenBy(h => h.Id)
                   .Select(h => new AreaHouseDto(h.Id, h.Title))
                   .ToList();

        var images = area.AreaImages
                    .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                    .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(area.Id, i.FileName)), null, "Gallery"))
                    .ToList();

        var cityItems = area.Cities
                    .OrderBy(c => c.Zip)
                    .ThenBy(c => c.Name)
                    .Select(c => new LookupItem(c.Id, $"{c.Zip} – {c.Name}"))
                    .ToList();

        var cityIds = cityItems.Select(c => c.Id).ToList();



        return new AreaDetailsDto(area.Id, area.Name, cityIds, cityItems, area.Description, images, Houses: houses);
    }
}
