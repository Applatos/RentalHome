using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Areas;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/areas")]
public sealed class AreasController(AppDbContext db) : ControllerBase
{
    // GET: list
    [HttpGet]
    public async Task<IEnumerable<AreaListItemDto>> GetAll(CancellationToken ct)
    {
        return await db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AreaListItemDto(
                a.Id,
                a.Slug,
                a.Name,
                a.Houses.Count,
                a.AreaImages.Count))
            .ToListAsync(ct);
    }

    // GET: detail
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var area = await db.Areas
            .Include(a => a.AreaImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null) return NotFound();

        var images = area.AreaImages
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new AreaImageItemDto(i.Id, UrlBuilder.AreaImageWebPath(area.Id, i.FileName)))
            .ToList();

        return new AreaDetailDto(area.Id, area.Slug, area.Name, area.Description, images);
    }

    // POST
    [HttpPost]
    public async Task<ActionResult<AreaDetailDto>> Create([FromBody] CreateAreaDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Navn er påkrævet");
            return ValidationProblem(ModelState);
        }

        var name = dto.Name.Trim();
        var area = new Area
        {
            Name = name,
            Slug = await GenerateUniqueSlugAsync(name, null, ct),
            Description = Clean(dto.Description),
            CityId = dto.CityId
        };

        if (dto.Images is { Count: > 0 })
        {
            area.AreaImages = dto.Images.Select(f => new AreaImage { FileName = f }).ToList();
        }

        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);

        var result = new AreaDetailDto(
            area.Id,
            area.Slug,
            area.Name,
            area.Description,
            area.AreaImages
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new AreaImageItemDto(i.Id, UrlBuilder.AreaImageWebPath(area.Id, i.FileName)))
                .ToList());

        return CreatedAtAction(nameof(Get), new { id = area.Id }, result);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAreaDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Navn er påkrævet");
            return ValidationProblem(ModelState);
        }

        // A) Slet billeder via raw SQL (virker i EF6/7/8)
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM AreaImages WHERE AreaId = {0}", id);

        // B) Opdater Area via "attach" så vi undgår at læsse grafen igen
        var stub = new Area { Id = id };
        db.Areas.Attach(stub);

        var name = dto.Name.Trim();
        stub.Name = name;
        stub.Slug = await GenerateUniqueSlugAsync(name, id, ct);
        stub.Description = Clean(dto.Description);
        stub.CityId = dto.CityId;

        db.Entry(stub).Property(a => a.Name).IsModified = true;
        db.Entry(stub).Property(a => a.Slug).IsModified = true;
        db.Entry(stub).Property(a => a.Description).IsModified = true;
        db.Entry(stub).Property(a => a.CityId).IsModified = true;

        await db.SaveChangesAsync(ct);

        // C) Indsæt nye billeder (AddRange)
        if (dto.Images is { Count: > 0 })
        {
            var images = dto.Images.Select(f => new AreaImage
            {
                AreaId = id,
                FileName = f,
                SortOrder = 0
            });
            db.AreaImages.AddRange(images);
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }




    // DELETE
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var area = await db.Areas.FindAsync(new object[] { id }, ct);
        if (area is null) return NotFound();

        db.Areas.Remove(area);
        await db.SaveChangesAsync(ct);
        return NoContent();
}

    private static readonly Regex SlugRegex = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Guid.NewGuid().ToString("N");
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        var cleaned = SlugRegex.Replace(sb.ToString(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? Guid.NewGuid().ToString("N") : cleaned;
    }

    private async Task<string> GenerateUniqueSlugAsync(string value, Guid? ignoreId, CancellationToken ct)
    {
        var baseSlug = Slugify(value);
        var slug = baseSlug;
        var suffix = 1;

        while (await db.Areas.AnyAsync(a => a.Slug == slug && (!ignoreId.HasValue || a.Id != ignoreId.Value), ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
