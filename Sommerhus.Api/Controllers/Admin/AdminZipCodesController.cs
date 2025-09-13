using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models; // ZipCode

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/zipcodes")]
public sealed class AdminZipCodesController(AppDbContext db) : ControllerBase
{
    public sealed record ZipDto(Guid Id, string Zip, string City);

    public sealed class ZipPageDto
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ZipDto> Items { get; set; } = new();
    }

    public sealed record CreateUpdateDto(string Zip, string City);

    [HttpGet]
    public async Task<ActionResult<ZipPageDto>> List(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = db.ZipCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var f = query.Trim();
            q = q.Where(z => z.Zip.Contains(f) || z.City.Contains(f));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(z => z.City).ThenBy(z => z.Zip)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(z => new ZipDto(z.Id, z.Zip, z.City))
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
    public async Task<ActionResult<ZipDto>> Create([FromBody] CreateUpdateDto dto, CancellationToken ct)
    {
        var z = new ZipCode { Zip = dto.Zip.Trim(), City = dto.City.Trim() };
        db.ZipCodes.Add(z);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = z.Id }, new ZipDto(z.Id, z.Zip, z.City));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ZipDto>> Get(Guid id, CancellationToken ct)
    {
        var z = await db.ZipCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        return new ZipDto(z.Id, z.Zip, z.City);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateDto dto, CancellationToken ct)
    {
        var z = await db.ZipCodes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        z.Zip = dto.Zip.Trim();
        z.City = dto.City.Trim();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var z = await db.ZipCodes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (z is null) return NotFound();
        db.ZipCodes.Remove(z);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
