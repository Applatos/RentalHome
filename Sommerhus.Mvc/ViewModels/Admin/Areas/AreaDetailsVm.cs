using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.Areas;

public sealed class AreaDetailsVm
{
    public required AreaDetailsDto Area { get; init; }
    public IReadOnlyList<SelectListItem> Cities { get; init; } = [];

    public string ActiveTab { get; init; } = "overview";

    public bool IsNew => Area.Id == Guid.Empty;
}
 