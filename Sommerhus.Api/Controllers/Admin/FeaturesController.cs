using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Features;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/features")]
public sealed class FeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDto>> GetAll(CancellationToken ct)
        => (await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct))
           .Select(f => new FeatureDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, f.SortOrder));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        var valueType = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        var feature = new Feature
        {
            Name = dto.Name,
            Key = dto.Key,
            ValueType = valueType,
            Unit = dto.Unit,
            IconUrl = dto.IconUrl,
            SortOrder = dto.SortOrder
        };

        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return Ok(feature.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFeatureDto dto, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        feature.Name = dto.Name;
        feature.Key = dto.Key;
        feature.ValueType = Enum.Parse<FeatureValueType>(dto.ValueType, true);
        feature.Unit = dto.Unit;
        feature.IconUrl = dto.IconUrl;
        feature.SortOrder = dto.SortOrder;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feature is null) return NotFound();

        db.Features.Remove(feature);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
