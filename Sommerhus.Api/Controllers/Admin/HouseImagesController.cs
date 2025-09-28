using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Data;
using Sommerhus.Api.Utils;
using System.IO;
using System.Linq;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses/{houseId:guid}/images")]
public class HouseImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> List(Guid houseId, CancellationToken ct)
    {
        var imgs = await db.Images
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);
        return imgs.Select(i =>
            new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()));
    }

    [HttpPost("{kind}")]
    [RequestSizeLimit(1024L * 1024L * 100L)]
    public async Task<ActionResult<ImageDto>> Upload(Guid houseId, string kind, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Fil er påkrævet");
            return ValidationProblem(ModelState);
        }

        var house = await db.Houses.Include(h => h.Images).FirstOrDefaultAsync(x => x.Id == houseId, ct);
        if (house is null) return NotFound();

        var imgKind = kind.ToLower() switch
        {
            "cover" or "coverimage" => ImageKind.Cover,
            "gallery" => ImageKind.Gallery,
            "floor" or "floorplan" => ImageKind.Floorplan,
            _ => ImageKind.Gallery
        };

        var dir = Path.Combine(env.WebRootPath, "uploads", "houses", houseId.ToString());
        Directory.CreateDirectory(dir);

        var unique = $"{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(file.FileName))}";
        var fullPath = Path.Combine(dir, unique);
        using (var fs = System.IO.File.Create(fullPath))
            await file.CopyToAsync(fs, ct);

        var img = new HouseImage { HouseId = houseId, FileName = unique, Kind = imgKind };
        db.Images.Add(img);

        if (imgKind == ImageKind.Cover)
        {
            foreach (var other in house.Images.Where(i => i.Id != img.Id && i.Kind == ImageKind.Cover))
            {
                other.Kind = ImageKind.Gallery;
            }
            house.CoverImageId = img.Id;
        }
        else if (imgKind == ImageKind.Floorplan)
        {
            foreach (var other in house.Images.Where(i => i.Id != img.Id && i.Kind == ImageKind.Floorplan))
            {
                other.Kind = ImageKind.Gallery;
            }
        }

        await db.SaveChangesAsync(ct);

        var url = UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(img.HouseId, img.FileName));
        return CreatedAtAction(nameof(List), new { houseId }, new ImageDto(img.Id, url, img.Alt, img.Kind.ToString()));
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();

        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is not null && house.CoverImageId == img.Id)
        {
            house.CoverImageId = null;
        }

        var path = Path.Combine(env.WebRootPath, "uploads", "houses", houseId.ToString(), img.FileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

        db.Images.Remove(img);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{imageId:guid}/set-cover")]
    public async Task<IActionResult> SetCover(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();

        var house = await db.Houses.Include(h => h.Images).FirstAsync(h => h.Id == houseId, ct);
        foreach (var other in house.Images.Where(i => i.Id != img.Id && i.Kind == ImageKind.Cover))
        {
            other.Kind = ImageKind.Gallery;
        }

        house.CoverImageId = img.Id;
        img.Kind = ImageKind.Cover;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{imageId:guid}")]
    public async Task<IActionResult> Get(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();
        var dir = Path.Combine(env.WebRootPath, "uploads", "houses", houseId.ToString());
        var fullPath = Path.Combine(dir, img.FileName);
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
        var contentType = "application/octet-stream"; // You may want to detect MIME type
        return File(bytes, contentType, img.FileName); // FIX: Use File() method correctly
    }
}
