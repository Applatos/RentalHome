using Sommerhus.Api.Dtos.Shared;  // FeatureValueDto, PostFeatureValueDto
using Sommerhus.Api.Models;
using Sommerhus.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/features")]
public class HouseFeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureValueDto>> Get(Guid houseId, CancellationToken ct)
    {
        var values = await db.HouseFeatureValues
            .Where(v => v.HouseId == houseId)
            .Include(v => v.Feature)
            .ToListAsync(ct);

        return values.Select(v =>
        {
            var f = v.Feature;
            return new FeatureValueDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, f.FormatValue(v.RawValue));
        });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(Guid houseId, [FromBody] List<PostFeatureValueDto> payload, CancellationToken ct)
    {
        var featureIds = payload.Select(i => i.FeatureId).ToHashSet();

        var existing = await db.HouseFeatureValues
            .Where(v => v.HouseId == houseId && featureIds.Contains(v.FeatureId))
            .ToListAsync(ct);

        // opdater eller indsæt
        foreach (var i in payload)
        {
            var row = existing.FirstOrDefault(v => v.FeatureId == i.FeatureId);
            if (row is null)
            {
                row = new HouseFeatureValue { HouseId = houseId, FeatureId = i.FeatureId };
                db.HouseFeatureValues.Add(row);
            }
            row.RawValue = i.RawValue;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public static class FeatureExtensions
{
    public static string FormatValue(this Feature feature, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return "";

        string disp = "";

        if (feature.ValueType == FeatureValueType.Bool)
        {
            if (bool.TryParse(rawValue, out var b))
            {
                disp = b ? "Ja" : "Nej";
            }
        }
        else if (feature.ValueType == FeatureValueType.Int)
        {
            disp = rawValue;
        }
        else if (feature.ValueType == FeatureValueType.Decimal)
        {
            disp = rawValue;
        }
        else if (feature.ValueType == FeatureValueType.Text)
        {
            disp = rawValue;
        }

        if (!string.IsNullOrWhiteSpace(feature.Unit) && !string.IsNullOrWhiteSpace(disp))
        {
            disp = $"{disp} {feature.Unit}";
        }

        return disp;
    }
}
