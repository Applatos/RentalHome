using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.Extensions;

public static class SelectListExtensions
{
    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items)
        => items.ToSelectList((IEnumerable<Guid>?)null);

    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items, Guid? selectedId)
        => items.ToSelectList(selectedId.HasValue ? new[] { selectedId.Value } : null);

    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items, IEnumerable<Guid>? selectedIds)
    {
        var selected = selectedIds is null ? new HashSet<Guid>() : selectedIds.ToHashSet();

        return items.Select(i => new SelectListItem
        {
            Value = i.Id.ToString(),
            Text = i.Label,
            Selected = selected.Contains(i.Id)
        });
    }
}
