// Sommerhus.Api/Controllers/Admin/CitiesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities")]
public sealed class CitiesController(AppDbContext db) : ControllerBase
{
    public sealed record CityListItem(Guid Id, string Name, string Zip, string? Slug);
    public sealed class PageDto
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<CityListItem> Items { get; set; } = new();
    }

    public sealed record CityDetails(Guid Id, string Name, string Zip, string? Slug, string? Text, string[] Images);

    [HttpGet]
    public async Task<PageDto> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);
        var qry = db.Cities.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            qry = qry.Where(c => c.Name.Contains(term) || c.Zip.Contains(term) || (c.Slug != null && c.Slug.Contains(term)));
        }

        var total = await qry.CountAsync(ct);
        var items = await qry.OrderBy(c => c.Zip).ThenBy(c => c.Name).ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CityListItem(c.Id, c.Name, c.Zip, c.Slug))
            .ToListAsync(ct);

        return new PageDto { Query = q, Page = page, PageSize = pageSize, Total = total, Items = items };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CityDetails>> Get(Guid id, CancellationToken ct)
    {
        var c = await db.Cities.Include(x => x.Images).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();

        var imgs = c.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => $"/uploads/areas/{c.Id}/{i.FileName}").ToArray();

        return new CityDetails(c.Id, c.Name, c.Zip, c.Slug, c.Text, imgs);
    }

    public sealed class CreateUpdate
    {
        public string name { get; set; } = "";
        public string zip { get; set; } = "";
        public string? slug { get; set; }
        public string? text { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateUpdate dto, CancellationToken ct)
    {
        var c = new City { Name = dto.name.Trim(), Zip = dto.zip.Trim(), Slug = dto.slug?.Trim(), Text = dto.text };
        db.Cities.Add(c);
        await db.SaveChangesAsync(ct);
        return Ok(c.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdate dto, CancellationToken ct)
    {
        var c = await db.Cities.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.Name = dto.name.Trim();
        c.Zip = dto.zip.Trim();
        c.Slug = dto.slug?.Trim();
        c.Text = dto.text;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var c = await db.Cities.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        db.Cities.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
