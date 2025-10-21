using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses/{houseId:guid}/images")]
public sealed class HouseImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> Get(Guid houseId, CancellationToken ct)
    {
        var imgs = await db.Images.AsNoTracking()
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : (i.Kind == ImageKind.Gallery ? 1 : 2))
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        return imgs.Select(i => new ImageDto(i.Id,
            UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)),
            i.Alt,
            i.Kind.ToString()));
    }

    // DELETE /api/admin/houses/{houseId}/images/{imageId}
    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();

        db.Images.Remove(img);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST: /api/admin/houses/{houseId}/images
    // multipart/form-data (name="files"; multiple)
    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<IEnumerable<ImageDto>>> Upload(Guid houseId, [FromForm] IFormFileCollection files, CancellationToken ct)
    {
        if (files is null || files.Count == 0) return BadRequest("No files.");
        var exists = await db.Houses.AnyAsync(h => h.Id == houseId, ct);
        if (!exists) return NotFound();

        var root = env.WebRootPath ?? "wwwroot";
        var dir = Path.Combine(root, "uploads", "houses", houseId.ToString());
        Directory.CreateDirectory(dir);

        var result = new List<ImageDto>();
        foreach (var f in files)
        {
            if (f.Length == 0) continue;
            if (string.IsNullOrWhiteSpace(f.ContentType) || !f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only images are allowed.");

            var ext = Path.GetExtension(f.FileName);
            var safe = $"{Guid.NewGuid():N}{ext}";
            var diskPath = Path.Combine(dir, safe);
            await using (var s = System.IO.File.Create(diskPath))
                await f.CopyToAsync(s, ct);

            var entity = new HouseImage { HouseId = houseId, FileName = safe, Kind = ImageKind.Gallery };
            db.Images.Add(entity);
            await db.SaveChangesAsync(ct);

            var url = UrlBuilder.HouseImageWebPath(houseId, safe);
            result.Add(new ImageDto(entity.Id, url, entity.Alt, entity.Kind.ToString()));
        }

        return Ok(result);
    }

    // POST: /api/admin/houses/{houseId}/images/{imageId}/set-kind?kind=Cover|Gallery|Floorplan
    [HttpPost("{imageId:guid}/set-kind")]
    public async Task<IActionResult> SetKind(Guid houseId, Guid imageId, [FromQuery] ImageKind kind, CancellationToken ct)
    {
        var imgs = await db.Images.Where(i => i.HouseId == houseId).ToListAsync(ct);
        var img = imgs.FirstOrDefault(i => i.Id == imageId);
        if (img is null) return NotFound();

        if (kind == ImageKind.Cover)
            foreach (var i in imgs.Where(i => i.Kind == ImageKind.Cover)) i.Kind = ImageKind.Gallery;

        if (kind == ImageKind.Floorplan)
            foreach (var i in imgs.Where(i => i.Kind == ImageKind.Floorplan)) i.Kind = ImageKind.Gallery;

        img.Kind = kind;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
