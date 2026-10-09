using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Common;

/// <summary>
/// The one order a house's images are shown in: the cover first, then the gallery in its sort
/// order, floor plans last. The id breaks ties, so images uploaded before sort orders existed
/// keep a stable order.
/// </summary>
public static class HouseImageOrder
{
    public static IOrderedQueryable<HouseImage> InDisplayOrder(this IQueryable<HouseImage> images)
        => images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.Id);

    public static IOrderedEnumerable<HouseImage> InDisplayOrder(this IEnumerable<HouseImage> images)
        => images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.Id);
}
