using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Services;
using System.Globalization;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseFeaturesController(AdminApiClient api) : AdminControllerBase
{
    [HttpPost("/admin/houses/{id:guid}/features")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHouseFeatures(Guid id, CancellationToken ct = default)
    {
        var featuresRes = await api.GetFeaturesAsync(ct);
        if (!featuresRes.Ok || featuresRes.Data is null)
        {
            SetError(featuresRes.Message ?? "Could not load features.");
            return RedirectToDetails(id, "features");
        }

        var form = await Request.ReadFormAsync(ct);
        var values = new List<PostFeatureValueDto>();
        var errors = new List<string>();

        foreach (var feature in featuresRes.Data)
        {
            var key = $"feature_{feature.Id}";
            if (!form.TryGetValue(key, out var formValue) || formValue.Count == 0)
            {
                continue;
            }

            var raw = formValue[^1]?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            switch (feature.ValueType)
            {
                case FeatureValueType.Bool:
                    if (IsTruthy(raw))
                    {
                        values.Add(new PostFeatureValueDto(feature.Id, "true"));
                    }
                    break;
                case FeatureValueType.Int:
                    if (!TryParseInt(raw, out var intValue))
                    {
                        errors.Add($"{feature.Name}: enter a whole number.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, intValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                case FeatureValueType.Decimal:
                    if (!TryParseDecimal(raw, out var decValue))
                    {
                        errors.Add($"{feature.Name}: enter a number.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, decValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                default:
                    if (raw.Length > 200)
                    {
                        errors.Add($"{feature.Name}: text too long (max 200 characters).");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, raw));
                    break;
            }
        }

        if (errors.Count > 0)
        {
            SetError(string.Join(" ", errors));
            return RedirectToDetails(id, "features");
        }

        var res = await api.UpsertHouseFeaturesAsync(id, values, ct);
        if (res.Ok)
        {
            SetSuccess("Features updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not save features.");
        }

        return RedirectToDetails(id, "features");
    }

    private static bool IsTruthy(string value)
        => value.Equals("true", StringComparison.OrdinalIgnoreCase)
           || value.Equals("on", StringComparison.OrdinalIgnoreCase)
           || value.Equals("1", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseInt(string input, out int value)
    {
        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            return true;
        return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDecimal(string input, out decimal value)
    {
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            return true;
        return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
