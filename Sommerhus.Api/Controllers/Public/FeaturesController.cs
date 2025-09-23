using Sommerhus.Api.Dtos.Admin.Features;
using Sommerhus.Api.Models; // for Feature, FeatureValueType
using Sommerhus.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDto>> GetAll(CancellationToken ct)
        => (await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct))
           .Select(f => new FeatureDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, f.SortOrder));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        var vt = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        var f = new Feature { Name = dto.Name, Key = dto.Key, ValueType = vt, Unit = dto.Unit, IconUrl = dto.IconUrl, SortOrder = dto.SortOrder };
        db.Features.Add(f);
        await db.SaveChangesAsync(ct);
        return Ok(f.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        var f = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null) return NotFound();

        f.Name = dto.Name;
        f.Key = dto.Key;
        f.ValueType = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        f.Unit = dto.Unit;
        f.IconUrl = dto.IconUrl;
        f.SortOrder = dto.SortOrder;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var f = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null) return NotFound();
        db.Features.Remove(f);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
