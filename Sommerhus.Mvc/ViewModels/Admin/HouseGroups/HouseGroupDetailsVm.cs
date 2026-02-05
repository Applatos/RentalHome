using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Admin.HouseGroups;

public sealed class HouseGroupDetailsVm
{
    public required HouseGroupDto Group { get; init; }
    public IReadOnlyList<SeasonCodeDto> SeasonCodes { get; init; } = [];
    public string Tab { get; init; } = "overview";
}
