using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.ViewModels.Public.Houses;

public sealed class HouseListVm
{
    public IReadOnlyList<PublicHouseListItemDto> Houses { get; init; } = [];
    public IReadOnlyList<AreaListItemDto> Areas { get; init; } = [];
    public Dictionary<string, IReadOnlyList<SearchableFeatureDto>> SearchableFeatures { get; init; } = [];
    public HouseSearchFilter Filter { get; init; } = new();

    // Backward-compat for existing views still using legacy properties.
    public string Query => Filter.Query ?? string.Empty;
    public string SelectedArea => Filter.AreaId?.ToString() ?? string.Empty;

    public int Total { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
