using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Mvc.ViewModels.Admin.Area;

public sealed class AreaCreateVm
{
    public UpsertAreaDto Area { get; set; } = new();
    public IReadOnlyList<SelectListItem> Cities { get; set; } = [];
}
