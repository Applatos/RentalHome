using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Shared;

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var area = await db.Areas.Include(a => a.AreaImages).Include(a => a.City)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        return MapDetails(area);
    }

    [HttpPost]
    public async Task<ActionResult<AreaDetailsDto>> Create([FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var name = (dto.Name ?? "").Trim();

        if (dto.CityId.HasValue)
        {
            var cityExists = await db.Cities.AnyAsync(c => c.Id == dto.CityId.Value, ct);
            if (!cityExists)
            {
                ModelState.AddModelError(nameof(dto.CityId), "Ukendt by");
                return ValidationProblem(ModelState);
            }
        }

        var area = new Area
        {
            Name = name,
            CityId = dto.CityId,
            Description = NormalizeDescription(dto.Description),
        };

        var images = NormalizeImages(dto.Images).ToList();
        if (images.Count > 0)
        {
            foreach (var img in images)
                img.AreaId = area.Id;
            area.AreaImages = images;
        }

        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);

        var created = await db.Areas.Include(a => a.AreaImages)
            .Include(a => a.City)
            .FirstAsync(a => a.Id == area.Id, ct);

        var details = MapDetails(created);
        return CreatedAtAction(nameof(Get), new { id = details.Id }, details);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertAreaDto dto, CancellationToken ct)
    {
        var area = await db.Areas.Include(a => a.AreaImages)
            .Include(a => a.City)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        var name = (dto.Name ?? "").Trim();

        if (dto.CityId.HasValue)
        {
            var cityExists = await db.Cities.AnyAsync(c => c.Id == dto.CityId.Value, ct);
            if (!cityExists)
            {
                ModelState.AddModelError(nameof(dto.CityId), "Ukendt by");
                return ValidationProblem(ModelState);
            }
        }

        var nameChanged = !string.Equals(area.Name, name, StringComparison.Ordinal);

        area.Name = name;
        area.CityId = dto.CityId;
        area.Description = NormalizeDescription(dto.Description);

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
            .Include(a => a.City)
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
        var images = area.AreaImages
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(area.Id, i.FileName)), null, "Gallery"))
            .ToList();

        return new AreaDetailsDto(area.Id, area.Name, area.CityId, area.City?.Name, area.Description, images);
    }

    //private static string Slugify(string value)
    //{
    //    var normalized = value.ToLowerInvariant().Trim();
    //    normalized = normalized.Normalize(NormalizationForm.FormD);
    //    var builder = new StringBuilder();
    //    foreach (var c in normalized)
    //    {
    //        var category = CharUnicodeInfo.GetUnicodeCategory(c);
    //        if (category == UnicodeCategory.NonSpacingMark) continue;
    //        builder.Append(c);
    //    }

    //    normalized = builder.ToString();
    //    return string.IsNullOrEmpty(normalized) ? Guid.NewGuid().ToString("N") : normalized;
    //}

    //private async Task<string> GenerateUniqueSlugAsync(string value, Guid? ignoreId, CancellationToken ct)
    //{
    //    var baseSlug = Slugify(value);
    //    var slug = baseSlug;
    //    var suffix = 2;

    //    while (await db.Areas.AnyAsync(a => a.Slug == slug && (!ignoreId.HasValue || a.Id != ignoreId.Value), ct))
    //        slug = $"{baseSlug}-{suffix++}";

    //    return slug;
    //}
}
