using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/features")]
public sealed class FeaturesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDto>> GetAll(CancellationToken ct)
        => (await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct))
            .Select(f => new FeatureDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, f.SortOrder));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<Models.FeatureValueType>(dto.ValueType, true, out var vt))
        {
            ModelState.AddModelError(nameof(dto.ValueType), "Ugyldig ValueType");
            return ValidationProblem(ModelState);
        }

        var key = (dto.Key ?? "").Trim().ToLowerInvariant();
        if (await db.Features.AnyAsync(f => f.Key == key, ct))
        {
            ModelState.AddModelError(nameof(dto.Key), "Key skal være unik");
            return ValidationProblem(ModelState);
        }

        var feature = new Models.Feature
        {
            Name = dto.Name.Trim(),
            Key = key,
            ValueType = vt,
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(dto.IconUrl) ? null : dto.IconUrl.Trim(),
            SortOrder = dto.SortOrder
        };

        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return Ok(feature.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        if (!Enum.TryParse<Models.FeatureValueType>(dto.ValueType, true, out var vt))
        {
            ModelState.AddModelError(nameof(dto.ValueType), "Ugyldig ValueType");
            return ValidationProblem(ModelState);
        }

        var key = (dto.Key ?? "").Trim().ToLowerInvariant();
        if (await db.Features.AnyAsync(f => f.Id != id && f.Key == key, ct))
        {
            ModelState.AddModelError(nameof(dto.Key), "Key skal være unik");
            return ValidationProblem(ModelState);
        }

        feature.Name = dto.Name.Trim();
        feature.Key = key;
        feature.ValueType = vt;
        feature.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();
        feature.IconUrl = string.IsNullOrWhiteSpace(dto.IconUrl) ? null : dto.IconUrl.Trim();
        feature.SortOrder = dto.SortOrder;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        // (valgfrit) slet evt. ikon fra disk, hvis du gemmer det fysisk
        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/icon")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Fil er påkrævet");

        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return NotFound();

        var folder = Path.Combine(env.WebRootPath, "uploads", "features", id.ToString());
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(Path.GetFileName(file.FileName));
        var safe = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, safe);
        using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        feature.IconUrl = UrlBuilder.FeatureIconWebPath(id, safe);
        await db.SaveChangesAsync(ct);

        return Ok(new { feature.IconUrl });
    }

    [HttpDelete("{id:guid}/icon")]
    public async Task<IActionResult> DeleteIcon(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return NotFound();

        feature.IconUrl = null;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
