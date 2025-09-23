using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Features;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses/{houseId:guid}/features")]
public sealed class HouseFeaturesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upsert(Guid houseId, [FromBody] List<PostFeatureValueDto> payload, CancellationToken ct)
    {
        if (payload is null || payload.Count == 0)
        {
            return NoContent();
        }

        var featureIds = payload.Select(i => i.FeatureId).ToHashSet();

        var existing = await db.HouseFeatureValues
            .Where(v => v.HouseId == houseId && featureIds.Contains(v.FeatureId))
            .ToListAsync(ct);

        foreach (var item in payload)
        {
            var row = existing.FirstOrDefault(v => v.FeatureId == item.FeatureId);
            if (row is null)
            {
                row = new HouseFeatureValue { HouseId = houseId, FeatureId = item.FeatureId };
                db.HouseFeatureValues.Add(row);
            }

            row.RawValue = item.RawValue;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
