using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using System.IO;
using System.Linq;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities/{cityId:guid}/images")]
public sealed class CityImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ImageDto>>> List(Guid cityId, CancellationToken ct)
    {
        var city = await db.Cities
            .Include(c => c.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cityId, ct);

        if (city is null) return NotFound();

        var images = city.Images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.CityImageWebPath(city.Id, i.FileName)), i.Alt, "city"))
            .ToList();

        return images;
    }

    [HttpPost]
    [RequestSizeLimit(1024L * 1024L * 100L)]
    public async Task<ActionResult<ImageDto>> Upload(Guid cityId, IFormFile file, [FromForm] string? alt, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Fil er påkrævet");
            return ValidationProblem(ModelState);
        }

        var city = await db.Cities.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == cityId, ct);
        if (city is null) return NotFound();

        var dir = Path.Combine(env.WebRootPath, "uploads", "cities", cityId.ToString());
        Directory.CreateDirectory(dir);

        var unique = $"{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(file.FileName))}";
        var path = Path.Combine(dir, unique);
        using (var fs = System.IO.File.Create(path))
            await file.CopyToAsync(fs, ct);

        var sortOrder = city.Images.Count == 0 ? 0 : city.Images.Max(i => i.SortOrder) + 10;
        var image = new CityImage
        {
            CityId = cityId,
            FileName = unique,
            Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim(),
            SortOrder = sortOrder
        };

        db.CityImages.Add(image);
        await db.SaveChangesAsync(ct);

        var dto = new ImageDto(image.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.CityImageWebPath(cityId, image.FileName)), image.Alt, "city");
        return CreatedAtAction(nameof(List), new { cityId }, dto);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid cityId, Guid imageId, CancellationToken ct)
    {
        var image = await db.CityImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CityId == cityId, ct);
        if (image is null) return NotFound();

        var path = Path.Combine(env.WebRootPath, "uploads", "cities", cityId.ToString(), image.FileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

        db.CityImages.Remove(image);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
