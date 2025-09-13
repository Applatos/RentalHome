using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Controllers.Models;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/houses/{houseId:guid}/images")]
public class HouseImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static string ImgPath(Guid houseId, string fileName)
        => $"/uploads/houses/{houseId}/{fileName}";

    private string AbsUrl(string relative)
        => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{relative}";

    private static ImageKind ParseKind(string kind)
        => kind.ToLower() switch
        {
            "cover" or "coverimage" => ImageKind.Cover,
            "gallery" => ImageKind.Gallery,
            "floor" or "floorplan" => ImageKind.Floorplan,
            _ => ImageKind.Gallery
        };

    [HttpGet]
    public async Task<IEnumerable<HouseImageDto>> List(Guid houseId, CancellationToken ct)
    {
        var imgs = await db.Images.Where(i => i.HouseId == houseId).ToListAsync(ct);
        return imgs.Select(i => new HouseImageDto(i.Id, AbsUrl(ImgPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()));
    }

    [HttpPost("{kind}")]
    [RequestSizeLimit(1024L * 1024L * 100L)] // 100 MB
    public async Task<ActionResult<HouseImageDto>> Upload(Guid houseId, string kind, IFormFile file, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(x => x.Id == houseId, ct);
        if (house is null) return NotFound();

        var imgKind = ParseKind(kind);
        var dir = Path.Combine(env.WebRootPath, "uploads", "houses", houseId.ToString());
        Directory.CreateDirectory(dir);

        var safeName = Path.GetFileName(file.FileName);
        var unique = $"{Guid.NewGuid():N}{Path.GetExtension(safeName)}";
        var fullPath = Path.Combine(dir, unique);
        using (var fs = System.IO.File.Create(fullPath))
            await file.CopyToAsync(fs, ct);

        var img = new HouseImage { HouseId = houseId, FileName = unique, Kind = imgKind };
        db.Images.Add(img);

        if (imgKind == ImageKind.Cover)
            house.CoverImageId = img.Id;

        await db.SaveChangesAsync(ct);

        var url = AbsUrl(ImgPath(img.HouseId, img.FileName));
        return Ok(new HouseImageDto(img.Id, url, img.Alt, img.Kind.ToString()));
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var img = await db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (img is null) return NotFound();

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

        var house = await db.Houses.FirstAsync(h => h.Id == houseId, ct);
        house.CoverImageId = img.Id;
        img.Kind = ImageKind.Cover;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
