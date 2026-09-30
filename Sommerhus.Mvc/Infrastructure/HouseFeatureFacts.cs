using System.Globalization;
using Sommerhus.Domain.Models;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>A feature with a value, shown as a key fact: "210 m²" captioned "Size".</summary>
public sealed record HouseFact(string Value, string Caption, string? IconUrl);

/// <summary>A yes/no feature the house has.</summary>
public sealed record HouseFacility(string Name, string? IconUrl);

/// <summary>
/// Splits a house's features into key facts (number and text features with a value) and
/// facilities (yes/no features that are yes). A feature that is "no", empty or unreadable is left
/// out. Both lists keep the order the API returned.
/// </summary>
public sealed record HouseFeatureFacts(IReadOnlyList<HouseFact> Facts, IReadOnlyList<HouseFacility> Facilities)
{
    private static readonly string[] TrueValues = ["1", "yes", "y", "ja", "true"];

    // Group separators are not accepted, so "12,5" is never read as 125.
    private const NumberStyles DecimalStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
        | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

    public bool IsEmpty => Facts.Count == 0 && Facilities.Count == 0;

    public static HouseFeatureFacts From(IEnumerable<FeatureValueDto>? features, CultureInfo culture)
    {
        var facts = new List<HouseFact>();
        var facilities = new List<HouseFacility>();

        foreach (var feature in features ?? [])
        {
            var raw = feature.RawValue?.Trim();
            if (string.IsNullOrEmpty(raw) || string.IsNullOrWhiteSpace(feature.Name))
            {
                continue;
            }

            var icon = string.IsNullOrWhiteSpace(feature.IconUrl) ? null : feature.IconUrl;
            if (feature.ValueType == FeatureValueType.Bool)
            {
                if (IsTrue(raw))
                {
                    facilities.Add(new HouseFacility(feature.Name.Trim(), icon));
                }
                continue;
            }

            var unit = feature.Unit?.Trim();
            var value = FormatValue(feature.ValueType, raw, culture);
            if (feature.ValueType != FeatureValueType.Text && !string.IsNullOrEmpty(unit))
            {
                value = $"{value} {unit}";
            }

            facts.Add(new HouseFact(value, Caption(feature.Name, unit), icon));
        }

        return new HouseFeatureFacts(facts, facilities);
    }

    private static bool IsTrue(string raw)
        => bool.TryParse(raw, out var parsed)
            ? parsed
            : TrueValues.Contains(raw, StringComparer.OrdinalIgnoreCase);

    // Values are stored in the invariant culture; a value typed with a local decimal comma is
    // still read rather than printed raw.
    private static string FormatValue(FeatureValueType type, string raw, CultureInfo culture)
    {
        switch (type)
        {
            case FeatureValueType.Int when long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole):
                return whole.ToString("N0", culture);
            case FeatureValueType.Decimal or FeatureValueType.Int
                when decimal.TryParse(raw, DecimalStyle, CultureInfo.InvariantCulture, out var number)
                  || decimal.TryParse(raw, DecimalStyle, culture, out number):
                return number.ToString("#,0.##", culture);
            default:
                return raw;
        }
    }

    // "Size (m²)" with unit "m²" is captioned "Size", so the unit is not printed twice.
    private static string Caption(string name, string? unit)
    {
        var caption = name.Trim();
        if (!string.IsNullOrEmpty(unit))
        {
            var suffix = $"({unit})";
            if (caption.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && caption.Length > suffix.Length)
            {
                caption = caption[..^suffix.Length].TrimEnd();
            }
        }

        return caption;
    }
}
