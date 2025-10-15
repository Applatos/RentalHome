using Sommerhus.Contracts.Dtos.Shared;
using System;
using System.Collections.Generic;
using Sommerhus.Contracts.Dtos.Admin.Features;
using System.Linq;

namespace Sommerhus.Mvc.Utils;

public static class EnumerableExtensions
{
    public static IEnumerable<ImageDto> WithoutCover(this IEnumerable<ImageDto> images)
        => images.Where(i => !string.Equals(i.Kind, "Cover", StringComparison.OrdinalIgnoreCase))
                 .GroupBy(i => i.Id).Select(g => g.First());

    public static IEnumerable<FeatureDetailsDto> DistinctFeatures(this IEnumerable<FeatureDetailsDto> features)
        => features.GroupBy(f => string.IsNullOrWhiteSpace(f.Key) ? f.Name : f.Key, StringComparer.OrdinalIgnoreCase)
                   .Select(g => g.First());
}
