// Sommerhus.Api/Controllers/Admin/FeaturesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/features")]
public sealed class FeaturesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDetailsDto>> GetAll(CancellationToken ct)
    {
        var items = await db.Features.AsNoTracking()
            .OrderBy(f => f.Name) // SortOrder droppes
            .ToListAsync(ct);

        return items.Select(f => new FeatureDetailsDto(
            f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit,
            f.IconUrl is null ? null : UrlBuilder.ToAbsolute(Request, f.IconUrl)
        ));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<FeatureValueType>(dto.ValueType, true, out var vt))
        {
            ModelState.AddModelError(nameof(dto.ValueType), "Ugyldig ValueType (tilladt: Bool, Int, Decimal, Text).");
            return ValidationProblem(ModelState);
        }

        dto = dto with { Key = dto.Key.Trim() };
        if (string.IsNullOrWhiteSpace(dto.Key) || dto.Key.Length > 60 || !dto.Key.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
        {
            ModelState.AddModelError(nameof(dto.Key), "Key skal være [a-zA-Z0-9_-], maks 60 tegn.");
            return ValidationProblem(ModelState);
        }

        var exists = await db.Features.AnyAsync(f => f.Key == dto.Key, ct);
        if (exists)
        {
            ModelState.AddModelError(nameof(dto.Key), "Key er allerede i brug.");
            return ValidationProblem(ModelState);
        }

        var feature = new Feature
        {
            Name = dto.Name.Trim(),
            Key = dto.Key,
            ValueType = vt,
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim(),
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

        if (!Enum.TryParse<FeatureValueType>(dto.ValueType, true, out var vt))
        {
            ModelState.AddModelError(nameof(dto.ValueType), "Ugyldig ValueType (tilladt: Bool, Int, Decimal, Text).");
            return ValidationProblem(ModelState);
        }

        var newKey = dto.Key.Trim();
        if (string.IsNullOrWhiteSpace(newKey) || newKey.Length > 60 || !newKey.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
        {
            ModelState.AddModelError(nameof(dto.Key), "Key skal være [a-zA-Z0-9_-], maks 60 tegn.");
            return ValidationProblem(ModelState);
        }

        var keyTaken = await db.Features.AnyAsync(f => f.Key == newKey && f.Id != id, ct);
        if (keyTaken)
        {
            ModelState.AddModelError(nameof(dto.Key), "Key er allerede i brug.");
            return ValidationProblem(ModelState);
        }

        feature.Name = dto.Name.Trim();
        feature.Key = newKey;
        feature.ValueType = vt;
        feature.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ===== Ikon upload/slet =====

    [HttpPost("{id:guid}/icon")]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        // Tjek at feature findes før upload
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        if (file is null || file.Length == 0) return BadRequest("Ingen fil modtaget.");
        if (file.Length > 2 * 1024 * 1024) return BadRequest("Fil er for stor (maks 2MB).");

        var allowed = new[] { "image/png", "image/jpeg" };
        if (file.ContentType is null || !allowed.Contains(file.ContentType))
            return BadRequest("Kun PNG og JPEG er tilladt.");

        // Gem filen
        var root = Path.Combine(env.WebRootPath, "uploads", "features", id.ToString());
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(root, safeName);

        var diskPath = UrlBuilder.FeatureIconDiskPath(env, id, safeName);
        Directory.CreateDirectory(Path.GetDirectoryName(diskPath)!);

        using (var stream = System.IO.File.Create(diskPath))
            await file.CopyToAsync(stream, ct);

        // Opdater database
        feature.IconUrl = UrlBuilder.FeatureIconWebPath(id, safeName);
        await db.SaveChangesAsync(ct);

        // Returner den absolutte URL
        var absolute = UrlBuilder.ToAbsolute(Request, feature.IconUrl);
        return Ok(new { iconUrl = absolute });
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
