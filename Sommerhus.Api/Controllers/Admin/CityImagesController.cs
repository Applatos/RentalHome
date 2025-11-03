using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities/{cityId:guid}/images")]
public sealed class CityImagesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ImageDto>>> List(Guid cityId, CancellationToken ct)
    {
        var city = await db.Cities
            .Include(c => c.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cityId, ct);

        if (city is null)
        {
            return NotFound();
        }

        var images = city.Images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(
                i.Id,
                storage.GetUrl(Request, ImageCategory.City, city.Id, i.FileName),
                i.Alt,
                "city"))
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
        if (city is null)
        {
            return NotFound();
        }

        var stored = await storage.SaveAsync(ImageCategory.City, cityId, file, ct);

        var sortOrder = city.Images.Count == 0 ? 0 : city.Images.Max(i => i.SortOrder) + 10;
        var image = new CityImage
        {
            CityId = cityId,
            FileName = stored.FileName,
            Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim(),
            SortOrder = sortOrder
        };

        db.CityImages.Add(image);
        await db.SaveChangesAsync(ct);

        var dto = new ImageDto(image.Id, storage.GetUrl(Request, ImageCategory.City, cityId, image.FileName), image.Alt, "city");
        return CreatedAtAction(nameof(List), new { cityId }, dto);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid cityId, Guid imageId, CancellationToken ct)
    {
        var image = await db.CityImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CityId == cityId, ct);
        if (image is null)
        {
            return NotFound();
        }

        db.CityImages.Remove(image);
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(ImageCategory.City, cityId, image.FileName, ct);
        return NoContent();
    }
}
