using Sommerhus.Api.Dtos.Admin.Areas;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Mapping;

public static class DtoMapping
{
    public static AreaDetailDto ToAreaDetail(this Area a) =>
        new(a.Id, a.Slug, a.Name, a.Description,
            a.AreaImages
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new AreaImageItemDto(i.Id, $"/uploads/areas/{a.Id}/{i.FileName}"))
                .ToList());

    public static AreaListItemDto ToAreaListItem(this Area a) =>
        new(a.Id, a.Slug, a.Name, a.Houses.Count, a.AreaImages.Count);
}
