using System.Globalization;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>A small icon with its label on a house card: "Spa", "100 m til stranden".</summary>
public sealed record HouseCardIcon(string IconUrl, string Label);

/// <summary>
/// What a house card says about a house, picked from its features by key: the key facts line
/// ("12 personer · 5 soveværelser · 225 m²") and up to four icons. The icons are the first
/// features (in the features' sort order) that have an icon, are not one of the key facts'
/// property features, and have a value worth showing.
/// </summary>
public static class HouseCardFacts
{
    private const int MaxIcons = 4;

    private static readonly string[] GuestKeys = ["max_guests", "guests", "maxguests"];

    public static string? FactsLine(IEnumerable<FeatureValueDto>? features, IStringLocalizer localizer, CultureInfo culture)
    {
        var list = features?.ToList() ?? [];
        var parts = new List<string>();

        if (Number(list, GuestKeys) is { } guests)
            parts.Add(localizer["Pages.HouseCard.Guests", guests.ToString("N0", culture)]);
        if (Number(list, "bedrooms") is { } bedrooms)
            parts.Add(localizer["Pages.HouseCard.Bedrooms", bedrooms.ToString("N0", culture)]);
        if (Find(list, "size_m2") is { } size && Number(size) is { } squareMetres)
            parts.Add($"{squareMetres.ToString("N0", culture)} {size.Unit ?? "m²"}".TrimEnd());

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>The house page's line under the title: "12 personer · 5 soveværelser · 2 badeværelser".</summary>
    public static string? RoomsLine(IEnumerable<FeatureValueDto>? features, IStringLocalizer localizer, CultureInfo culture)
    {
        var list = features?.ToList() ?? [];
        var parts = new List<string>();

        if (Number(list, GuestKeys) is { } guests)
            parts.Add(localizer["Pages.HouseCard.Guests", guests.ToString("N0", culture)]);
        if (Number(list, "bedrooms") is { } bedrooms)
            parts.Add(localizer["Pages.HouseCard.Bedrooms", bedrooms.ToString("N0", culture)]);
        if (Number(list, "bathrooms") is { } bathrooms)
            parts.Add(localizer["Pages.House.Bathrooms", bathrooms.ToString("N0", culture)]);

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    public static IReadOnlyList<HouseCardIcon> Icons(IEnumerable<FeatureValueDto>? features, CultureInfo culture)
    {
        var icons = new List<HouseCardIcon>();
        foreach (var feature in features ?? [])
        {
            if (icons.Count == MaxIcons)
                break;
            if (string.IsNullOrWhiteSpace(feature.IconUrl) || feature.Category == FeatureCategory.Property)
                continue;

            var raw = feature.RawValue?.Trim();
            if (string.IsNullOrEmpty(raw))
                continue;

            if (feature.ValueType == FeatureValueType.Bool)
            {
                if (bool.TryParse(raw, out var yes) && yes)
                    icons.Add(new HouseCardIcon(feature.IconUrl, feature.Name));
                continue;
            }

            // "100 m til stranden": the value, its unit, then the name as the rest of the phrase.
            var value = Number(feature) is { } number ? number.ToString("N0", culture) : raw;
            var unit = string.IsNullOrWhiteSpace(feature.Unit) ? string.Empty : $" {feature.Unit.Trim()}";
            icons.Add(new HouseCardIcon(feature.IconUrl, $"{value}{unit} {LowerFirst(feature.Name, culture)}"));
        }

        return icons;
    }

    private static FeatureValueDto? Find(IEnumerable<FeatureValueDto> features, params string[] keys)
        => features.FirstOrDefault(f => keys.Contains(f.Key, StringComparer.OrdinalIgnoreCase));

    private static long? Number(IEnumerable<FeatureValueDto> features, params string[] keys)
        => Find(features, keys) is { } feature ? Number(feature) : null;

    private static long? Number(FeatureValueDto feature)
        => long.TryParse(feature.RawValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static string LowerFirst(string text, CultureInfo culture)
        => string.IsNullOrEmpty(text) ? text : char.ToLower(text[0], culture) + text[1..];
}
