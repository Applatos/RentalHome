using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Controllers.Models;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/houses/{houseId:guid}/features")]
public class HouseFeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureValueDto>> Get(Guid houseId, CancellationToken ct)
    {
        var values = await db.HouseFeatureValues
            .Include(v => v.Feature)
            .Where(v => v.HouseId == houseId)
            .ToListAsync(ct);

        return values.Select(v =>
        {
            var f = v.Feature!;
            var disp = f.ValueType switch
            {
                FeatureValueType.Bool => v.ValueBool == true ? "Ja" : "Nej",
                FeatureValueType.Int => v.ValueInt?.ToString() ?? "",
                FeatureValueType.Decimal => v.ValueDecimal?.ToString() ?? "",
                FeatureValueType.Text => v.ValueText ?? "",
                _ => ""
            };
            if (!string.IsNullOrWhiteSpace(f.Unit) && !string.IsNullOrWhiteSpace(disp))
                disp = $"{disp} {f.Unit}";
            return new FeatureValueDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl,
                v.ValueBool, v.ValueInt, v.ValueDecimal, v.ValueText, disp);
        });
    }

    public record UpsertPayload(List<CreateHouseFeatureValueDto> Items);

    [HttpPost]
    public async Task<IActionResult> Upsert(Guid houseId, [FromBody] UpsertPayload payload, CancellationToken ct)
    {
        var featureIds = payload.Items.Select(i => i.FeatureId).ToHashSet();

        var existing = await db.HouseFeatureValues
            .Where(v => v.HouseId == houseId && featureIds.Contains(v.FeatureId))
            .ToListAsync(ct);

        // opdater eller indsæt
        foreach (var i in payload.Items)
        {
            var row = existing.FirstOrDefault(v => v.FeatureId == i.FeatureId);
            if (row is null)
            {
                row = new HouseFeatureValue { HouseId = houseId, FeatureId = i.FeatureId };
                db.HouseFeatureValues.Add(row);
            }
            row.ValueBool = i.ValueBool;
            row.ValueInt = i.ValueInt;
            row.ValueDecimal = i.ValueDecimal;
            row.ValueText = i.ValueText;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
