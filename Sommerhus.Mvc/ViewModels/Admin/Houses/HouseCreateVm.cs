using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Mvc.ViewModels.Admin.Houses;

public sealed class HouseCreateVm
{
    public UpsertHouseDto House { get; set; } = new();
    public IReadOnlyList<SelectListItem> Cities { get; set; } = [];
    public IReadOnlyList<SelectListItem> Areas { get; set; } = [];
    public IReadOnlyList<SelectListItem> HouseGroups { get; set; } = [];
}
