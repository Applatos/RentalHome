using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/areas/{areaId:guid}/images")]
public sealed class AreaImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static string Folder(Guid areaId) => Path.Combine("uploads", "areas", areaId.ToString());

    [HttpGet]
    public async Task<IEnumerable<object>> List(Guid areaId, CancellationToken ct)
    {
        var imgs = await db.AreaImages.Where(i => i.AreaId == areaId)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new
            {
                i.Id,
                Url = UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(areaId, i.FileName))
            })
            .ToListAsync(ct);

        return imgs;
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<object>> Upload(Guid areaId, IFormFile file, CancellationToken ct)
    {
        var exists = await db.Areas.AnyAsync(a => a.Id == areaId, ct);
        if (!exists) return NotFound();

        if (file is null || file.Length == 0) return BadRequest("Tom fil");

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var relFolder = Folder(areaId);
        var absFolder = Path.Combine(env.WebRootPath, relFolder);
        Directory.CreateDirectory(absFolder);

        var absPath = Path.Combine(absFolder, safeName);
        using (var fs = System.IO.File.Create(absPath))
            await file.CopyToAsync(fs, ct);

        var img = new AreaImage { AreaId = areaId, FileName = safeName, SortOrder = 0 };
        db.AreaImages.Add(img);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            img.Id,
            Url = UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(areaId, img.FileName))
        });
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid areaId, Guid imageId, CancellationToken ct)
    {
        var img = await db.AreaImages.FirstOrDefaultAsync(i => i.Id == imageId && i.AreaId == areaId, ct);
        if (img is null) return NotFound();

        db.AreaImages.Remove(img);
        await db.SaveChangesAsync(ct);

        var abs = Path.Combine(env.WebRootPath, Folder(areaId), img.FileName);
        if (System.IO.File.Exists(abs)) System.IO.File.Delete(abs);

        return NoContent();
    }
}
