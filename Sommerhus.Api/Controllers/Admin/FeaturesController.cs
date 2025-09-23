using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Features;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;

using System.IO;

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
        var valueType = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        var feature = new Feature
        {
            Name = dto.Name,
            Key = dto.Key,
            ValueType = valueType,
            Unit = dto.Unit,
            IconUrl = Clean(dto.IconUrl),
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

        feature.Name = dto.Name;
        feature.Key = dto.Key;
        feature.ValueType = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        feature.Unit = dto.Unit;
        feature.IconUrl = Clean(dto.IconUrl);
        feature.SortOrder = dto.SortOrder;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        var oldPath = ResolveIconPath(feature.IconUrl);
        if (oldPath is not null && System.IO.File.Exists(oldPath))
        {
            System.IO.File.Delete(oldPath);
        }

        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/icon")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Fil er påkrævet");
        }

        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return NotFound();

        var folder = Path.Combine(env.WebRootPath, "uploads", "features", id.ToString());
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, safeName);
        using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var oldPath = ResolveIconPath(feature.IconUrl);
        if (oldPath is not null && System.IO.File.Exists(oldPath))
        {
            System.IO.File.Delete(oldPath);
        }

        feature.IconUrl = UrlBuilder.FeatureIconWebPath(id, safeName);
        await db.SaveChangesAsync(ct);

        return Ok(new { feature.IconUrl });
    }

    [HttpDelete("{id:guid}/icon")]
    public async Task<IActionResult> DeleteIcon(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null) return NotFound();

        var oldPath = ResolveIconPath(feature.IconUrl);
        if (oldPath is not null && System.IO.File.Exists(oldPath))
        {
            System.IO.File.Delete(oldPath);
        }

        feature.IconUrl = null;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private string? ResolveIconPath(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;

        string relative = stored;
        if (Uri.TryCreate(stored, UriKind.Absolute, out var uri))
        {
            relative = uri.LocalPath;
        }

        relative = relative.TrimStart('/');
        if (!relative.StartsWith("uploads/features/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = relative.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(env.WebRootPath, normalized);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
