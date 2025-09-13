// Sommerhus.Api/Controllers/Admin/CityImagesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities/{cityId:guid}/images")]
public sealed class CityImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static string Folder(Guid cityId) => Path.Combine("uploads", "areas", cityId.ToString());
    private static string WebPath(Guid cityId, string fileName) => $"/uploads/areas/{cityId}/{fileName}";

    [HttpGet]
    public async Task<IEnumerable<object>> List(Guid cityId, CancellationToken ct)
    {
        var imgs = await db.CityImages.Where(i => i.CityId == cityId)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new { i.Id, Url = WebPath(cityId, i.FileName) })
            .ToListAsync(ct);
        return imgs;
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<object>> Upload(Guid cityId, IFormFile file, CancellationToken ct)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == cityId, ct);
        if (city is null) return NotFound();

        if (file is null || file.Length == 0) return BadRequest("Tom fil");

        var ext = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var relFolder = Folder(cityId);
        var absFolder = Path.Combine(env.WebRootPath, relFolder);
        Directory.CreateDirectory(absFolder);

        var absPath = Path.Combine(absFolder, safeName);
        using (var fs = System.IO.File.Create(absPath))
            await file.CopyToAsync(fs, ct);

        var img = new CityImage { CityId = cityId, FileName = safeName, SortOrder = 0 };
        db.CityImages.Add(img);
        await db.SaveChangesAsync(ct);

        return Ok(new { img.Id, Url = WebPath(cityId, img.FileName) });
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid cityId, Guid imageId, CancellationToken ct)
    {
        var img = await db.CityImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CityId == cityId, ct);
        if (img is null) return NotFound();

        db.CityImages.Remove(img);
        await db.SaveChangesAsync(ct);

        var abs = Path.Combine(env.WebRootPath, Folder(cityId), img.FileName);
        if (System.IO.File.Exists(abs)) System.IO.File.Delete(abs);

        return NoContent();
    }
    // Sommerhus.Api/Controllers/CitiesController.cs  (tilføj nederst i klassen)
    public sealed record AreaDetailsDto(string Slug, string Title, string Html, string[] Images);

    [HttpGet("{slug}")]
    public async Task<ActionResult<AreaDetailsDto>> GetBySlug(string slug, CancellationToken ct)
    {
        var c = await db.Cities.Include(x => x.Images).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (c is null) return NotFound();

        // meget simpel tekst->HTML (afsnit med <p>, linjeskift -> <br/>)
        string HtmlFromPlain(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "<p></p>";
            var parts = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
            var sb = new System.Text.StringBuilder();
            foreach (var p in parts)
            {
                var lines = p.Split('\n');
                var para = string.Join("<br/>", lines.Select(System.Net.WebUtility.HtmlEncode));
                sb.Append("<p>").Append(para).Append("</p>");
            }
            return sb.ToString();
        }

        var imgs = c.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => $"/uploads/areas/{c.Id}/{i.FileName}").ToArray();

        return new AreaDetailsDto(c.Slug ?? c.Name, c.Name, HtmlFromPlain(c.Text), imgs);
    }

}


