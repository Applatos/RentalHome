using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/areas/{areaId:guid}/images")]
public sealed class AreaImagesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> List(Guid areaId, CancellationToken ct)
    {
        var images = await db.AreaImages.AsNoTracking()
            .Where(i => i.AreaId == areaId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.FileName })
            .ToListAsync(ct);

        return images.Select(i => new ImageDto(
            i.Id,
            storage.GetUrl(Request, ImageCategory.Area, areaId, i.FileName),
            null,
            "Gallery"));
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImageDto>> Upload(Guid areaId, IFormFile file, CancellationToken ct)
    {
        var exists = await db.Areas.AsNoTracking().AnyAsync(a => a.Id == areaId, ct);
        if (!exists)
        {
            return NotFound();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("Tom fil");
        }

        var stored = await storage.SaveAsync(ImageCategory.Area, areaId, file, ct);

        var image = new AreaImage { AreaId = areaId, FileName = stored.FileName, SortOrder = 0 };
        db.AreaImages.Add(image);
        await db.SaveChangesAsync(ct);

        var dto = new ImageDto(image.Id, storage.GetUrl(Request, ImageCategory.Area, areaId, image.FileName), null, "Gallery");
        return CreatedAtAction(nameof(List), new { areaId }, dto);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid areaId, Guid imageId, CancellationToken ct)
    {
        var image = await db.AreaImages.FirstOrDefaultAsync(i => i.Id == imageId && i.AreaId == areaId, ct);
        if (image is null)
        {
            return NotFound();
        }

        db.AreaImages.Remove(image);
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(ImageCategory.Area, areaId, image.FileName, ct);

        return NoContent();
    }
}
