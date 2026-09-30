using System.Globalization;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Common;

/// <summary>
/// Reads a house's guest capacity from its max_guests feature value.
/// </summary>
public static class HouseCapacity
{
    /// <summary>
    /// Feature keys that hold the capacity, in priority order.
    /// </summary>
    public static readonly IReadOnlyList<string> FeatureKeys = new[] { "max_guests", "guests", "maxguests" };

    /// <summary>
    /// Returns the capacity, or null when the house has no usable value.
    /// The features' <see cref="HouseFeatureValue.Feature"/> must be loaded.
    /// </summary>
    public static int? MaxGuests(IEnumerable<HouseFeatureValue> features)
    {
        var list = features as IReadOnlyCollection<HouseFeatureValue> ?? features.ToList();

        foreach (var key in FeatureKeys)
        {
            var value = list
                .FirstOrDefault(f => string.Equals(f.Feature?.Key, key, StringComparison.OrdinalIgnoreCase))
                ?.RawValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : null;
        }

        return null;
    }
}
