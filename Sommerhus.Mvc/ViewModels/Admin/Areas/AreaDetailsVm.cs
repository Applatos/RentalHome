using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Areas;

public sealed class AreaDetailsVm
{
    public required AreaDetailsDto Area { get; init; }
    public IReadOnlyList<ImageDto> GalleryImages { get; init; } = [];
    public string Tab { get; init; } = "overview";
}
