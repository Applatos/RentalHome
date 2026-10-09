using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.ModelBinding;
using Sommerhus.Mvc.Services;
using System.Globalization;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseFeaturesController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/houses/{id:guid}/features")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHouseFeatures(Guid id, CancellationToken ct = default)
    {
        var featuresRes = await api.GetFeaturesAsync(ct);
        if (!featuresRes.Ok || featuresRes.Data is null)
        {
            SetError(featuresRes.Message ?? localizer["Admin.Features.Messages.LoadFailed"].Value);
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
                        errors.Add(localizer["Admin.Houses.Features.WholeNumber", feature.Name].Value);
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, intValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                case FeatureValueType.Decimal:
                    if (!TryParseDecimal(raw, out var decValue))
                    {
                        errors.Add(localizer["Admin.Houses.Features.Number", feature.Name].Value);
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, decValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                default:
                    if (raw.Length > 200)
                    {
                        errors.Add(localizer["Admin.Houses.Features.TextTooLong", feature.Name].Value);
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
            SetSuccess(localizer["Admin.Houses.Features.Updated"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Houses.Features.SaveFailed"].Value);
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

    // Not the request culture alone: under da-DK "." groups thousands, so "1.5" became 15.
    private static bool TryParseDecimal(string input, out decimal value)
        => CultureSafeNumber.TryParseDecimal(input, out value);
}
