using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Areas;

public sealed class AreaEditVm
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> CityIds { get; set; } = [];
    public string? Description { get; set; }
    public IReadOnlyList<ImageDto> Images { get; init; } = [];
    public IReadOnlyList<SelectListItem> Cities { get; init; } = [];

    public bool IsNew => !Id.HasValue || Id == Guid.Empty;
}

public sealed record AreaGalleryVm(Guid AreaId, IReadOnlyList<ImageDto> Images, string? RedirectTo = null);
