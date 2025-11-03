using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Storage;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses/{houseId:guid}/images")]
public sealed class HouseImagesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> Get(Guid houseId, CancellationToken ct)
    {
        var images = await db.Images.AsNoTracking()
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.FileName, i.Alt, i.Kind })
            .ToListAsync(ct);

        return images.Select(i => new ImageDto(
            i.Id,
            storage.GetUrl(Request, ImageCategory.House, houseId, i.FileName),
            i.Alt,
            i.Kind.ToString()));
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var image = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (image is null)
        {
            return NotFound();
        }

        db.Images.Remove(image);
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(ImageCategory.House, houseId, image.FileName, ct);
        return NoContent();
    }

    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<IEnumerable<ImageDto>>> Upload(Guid houseId, [FromForm] IFormFileCollection files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest("No files.");
        }

        var exists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!exists)
        {
            return NotFound();
        }

        var addedImages = new List<HouseImage>();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only images are allowed.");
            }

            var stored = await storage.SaveAsync(ImageCategory.House, houseId, file, ct);
            addedImages.Add(new HouseImage
            {
                HouseId = houseId,
                FileName = stored.FileName,
                Kind = ImageKind.Gallery
            });
        }

        if (addedImages.Count == 0)
        {
            return BadRequest("No valid files.");
        }

        await db.Images.AddRangeAsync(addedImages, ct);
        await db.SaveChangesAsync(ct);

        var result = addedImages
            .Select(img => new ImageDto(
                img.Id,
                storage.GetUrl(Request, ImageCategory.House, houseId, img.FileName),
                img.Alt,
                img.Kind.ToString()))
            .ToList();

        return Ok(result);
    }

    [HttpPost("{imageId:guid}/set-kind")]
    public async Task<IActionResult> SetKind(Guid houseId, Guid imageId, [FromQuery] ImageKind kind, CancellationToken ct)
    {
        var images = await db.Images.Where(i => i.HouseId == houseId).ToListAsync(ct);
        var image = images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
        {
            return NotFound();
        }

        if (kind == ImageKind.Cover)
        {
            foreach (var existing in images.Where(i => i.Kind == ImageKind.Cover))
            {
                existing.Kind = ImageKind.Gallery;
            }
        }

        if (kind == ImageKind.Floorplan)
        {
            foreach (var existing in images.Where(i => i.Kind == ImageKind.Floorplan))
            {
                existing.Kind = ImageKind.Gallery;
            }
        }

        image.Kind = kind;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
