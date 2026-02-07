using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Public.Houses;

public sealed class HouseListVm
{
    public IReadOnlyList<PublicHouseListItemDto> Houses { get; init; } = [];
    public IReadOnlyList<AreaListItemDto> Areas { get; init; } = [];
    public string Query { get; init; } = string.Empty;
    public string SelectedArea { get; init; } = string.Empty;
}
