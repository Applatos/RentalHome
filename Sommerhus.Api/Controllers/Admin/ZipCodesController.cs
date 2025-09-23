using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Cities;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/zipcodes")]
public sealed class ZipCodesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ZipPageDto>> List(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var f = query.Trim();
            q = q.Where(z => z.Zip.Contains(f) || z.Name.Contains(f));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(z => z.Name).ThenBy(z => z.Zip)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(z => new ZipDto(z.Id, z.Zip, z.Name))
            .ToListAsync(ct);

        return new ZipPageDto
        {
            Query = query,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    [HttpPost]
    public async Task<ActionResult<ZipDto>> Create([FromBody] CreateZipDto dto, CancellationToken ct)
    {
        var z = new City { Zip = dto.Zip.Trim(), Name = dto.City.Trim() };
        db.Cities.Add(z);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = z.Id }, new ZipDto(z.Id, z.Zip, z.Name));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ZipDto>> Get(Guid id, CancellationToken ct)
    {
        var z = await db.Cities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        return new ZipDto(z.Id, z.Zip, z.Name);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateZipDto dto, CancellationToken ct)
    {
        var z = await db.Cities.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        z.Zip = dto.Zip.Trim();
        z.Name = dto.City.Trim();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var z = await db.Cities.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        db.Cities.Remove(z);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
